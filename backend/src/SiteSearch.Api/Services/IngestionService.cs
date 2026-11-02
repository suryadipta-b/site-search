using System.Text.Json;
using System.Xml.Linq;
using SiteSearch.Api.Models;
using SiteSearch.Api.Storage;

namespace SiteSearch.Api.Services;

public interface IIngestionService
{
    IngestionSummary IngestAll(string? projectDocsPath = null, string? oshaXmlPath = null);
    IngestionSummary IngestProjectDocuments(string path);
    IngestionSummary IngestOshaXml(string path);
}

public record IngestionSummary(int IngestedCount, int SkippedCount, int TotalStoreCount, List<string> Errors);

public class IngestionService : IIngestionService
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(IDocumentStore documentStore, ILogger<IngestionService> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public IngestionSummary IngestAll(string? projectDocsPath = null, string? oshaXmlPath = null)
    {
        projectDocsPath ??= FindDataFile("project-documents.json");
        oshaXmlPath ??= FindDataFile("osha-title-29.xml");

        var errors = new List<string>();
        int totalIngested = 0;
        int totalSkipped = 0;

        _logger.LogInformation("Starting idempotent ingestion. ProjectDocs={Proj}, OshaXml={Osha}", projectDocsPath, oshaXmlPath);

        if (File.Exists(projectDocsPath))
        {
            var res = IngestProjectDocuments(projectDocsPath);
            totalIngested += res.IngestedCount;
            totalSkipped += res.SkippedCount;
            errors.AddRange(res.Errors);
        }
        else
        {
            var msg = $"Project documents file not found at: {projectDocsPath}";
            _logger.LogWarning(msg);
            errors.Add(msg);
        }

        if (File.Exists(oshaXmlPath))
        {
            var res = IngestOshaXml(oshaXmlPath);
            totalIngested += res.IngestedCount;
            totalSkipped += res.SkippedCount;
            errors.AddRange(res.Errors);
        }
        else
        {
            var msg = $"OSHA regulations XML file not found at: {oshaXmlPath}";
            _logger.LogWarning(msg);
            errors.Add(msg);
        }

        // Persist to disk store
        _documentStore.SaveToDisk();

        _logger.LogInformation("Ingestion complete. Ingested: {Ingested}, Skipped: {Skipped}, Total in store: {Total}",
            totalIngested, totalSkipped, _documentStore.Count);

        return new IngestionSummary(totalIngested, totalSkipped, _documentStore.Count, errors);
    }

    public IngestionSummary IngestProjectDocuments(string path)
    {
        int ingested = 0;
        int skipped = 0;
        var errors = new List<string>();

        try
        {
            var json = File.ReadAllText(path);
            var docs = JsonSerializer.Deserialize<List<Document>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (docs == null)
            {
                errors.Add("JSON file contained null or empty document array");
                return new IngestionSummary(0, 0, _documentStore.Count, errors);
            }

            foreach (var doc in docs)
            {
                // Validate record
                if (string.IsNullOrWhiteSpace(doc.Id) || string.IsNullOrWhiteSpace(doc.Number) || string.IsNullOrWhiteSpace(doc.Title))
                {
                    _logger.LogWarning("Skipping malformed project document record: Id={Id}, Number={Number}, Title={Title}", doc.Id, doc.Number, doc.Title);
                    skipped++;
                    errors.Add($"Malformed document skipped: Id={doc.Id ?? "null"}");
                    continue;
                }

                _documentStore.Upsert(doc);
                ingested++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting project documents from {Path}", path);
            errors.Add($"Error reading project documents: {ex.Message}");
        }

        return new IngestionSummary(ingested, skipped, _documentStore.Count, errors);
    }

    public IngestionSummary IngestOshaXml(string path)
    {
        int ingested = 0;
        int skipped = 0;
        var errors = new List<string>();

        try
        {
            var xdoc = XDocument.Load(path);

            // In eCFR XML, each section is a <DIV8 TYPE="SECTION" N="1926.451"> element inside a <DIV6 TYPE="SUBPART">
            var subparts = xdoc.Descendants()
                .Where(e => string.Equals((string?)e.Attribute("TYPE"), "SUBPART", StringComparison.OrdinalIgnoreCase));

            foreach (var subpart in subparts)
            {
                var subpartHead = subpart.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "HEAD", StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? "OSHA Safety Regulations";

                var sections = subpart.Descendants()
                    .Where(e => string.Equals((string?)e.Attribute("TYPE"), "SECTION", StringComparison.OrdinalIgnoreCase));

                foreach (var sec in sections)
                {
                    var sectionNumber = (string?)sec.Attribute("N");
                    if (string.IsNullOrWhiteSpace(sectionNumber))
                    {
                        _logger.LogWarning("Skipping OSHA section with missing attribute N");
                        skipped++;
                        continue;
                    }

                    var headElem = sec.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "HEAD", StringComparison.OrdinalIgnoreCase));
                    var title = headElem?.Value?.Trim() ?? $"OSHA {sectionNumber}";

                    // Collect all paragraphs <P>
                    var paragraphs = sec.Descendants()
                        .Where(e => string.Equals(e.Name.LocalName, "P", StringComparison.OrdinalIgnoreCase))
                        .Select(p => p.Value.Trim())
                        .Where(p => !string.IsNullOrEmpty(p));

                    var body = string.Join("\n\n", paragraphs);

                    var docId = $"reg-{sectionNumber.Replace(".", "-").ToLowerInvariant()}";

                    var doc = new Document
                    {
                        Id = docId,
                        ProjectId = null, // Global regulations
                        Type = "Regulation",
                        Number = sectionNumber.Trim(),
                        Title = title,
                        Body = body,
                        Discipline = "HSE",
                        CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                        Subpart = subpartHead
                    };

                    _documentStore.Upsert(doc);
                    ingested++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting OSHA XML from {Path}", path);
            errors.Add($"Error reading OSHA XML: {ex.Message}");
        }

        return new IngestionSummary(ingested, skipped, _documentStore.Count, errors);
    }

    private static string FindDataFile(string filename)
    {
        // Check current directory, data/, ../../data, etc.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data", filename),
            Path.Combine(AppContext.BaseDirectory, filename),
            Path.Combine(Directory.GetCurrentDirectory(), "data", filename),
            Path.Combine(Directory.GetCurrentDirectory(), filename),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data", filename),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "data", filename)
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c))
                return Path.GetFullPath(c);
        }

        return Path.Combine("data", filename);
    }
}
