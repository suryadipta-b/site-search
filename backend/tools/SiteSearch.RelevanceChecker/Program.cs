using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SiteSearch.Api.Models;
using SiteSearch.Api.Services;
using SiteSearch.Api.Storage;

Console.WriteLine("================================================================================");
Console.WriteLine("                  VConstruct Site Search - Relevance Check (B4)");
Console.WriteLine("================================================================================");

// Resolve base paths
var baseDir = AppContext.BaseDirectory;
string FindPath(string rel)
{
    var candidates = new[]
    {
        Path.Combine(baseDir, rel),
        Path.Combine(Directory.GetCurrentDirectory(), rel),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", rel),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", rel),
        Path.Combine(Directory.GetCurrentDirectory(), "..", rel)
    };
    return candidates.FirstOrDefault(File.Exists) ?? Path.GetFullPath(rel);
}

var casesPath = FindPath("relevance-cases.json");
var synonymsPath = FindPath("data/synonyms.json");
var projDocsPath = FindPath("data/project-documents.json");
var oshaXmlPath = FindPath("data/osha-title-29.xml");

Console.WriteLine($"[Config] Relevance Cases: {casesPath}");
Console.WriteLine($"[Config] Synonyms:        {synonymsPath}");
Console.WriteLine($"[Config] Project Docs:    {projDocsPath}");
Console.WriteLine($"[Config] OSHA XML:        {oshaXmlPath}");
Console.WriteLine();

// Initialize engine services
var tempStorePath = Path.Combine(Path.GetTempPath(), $"relevance_store_{Guid.NewGuid():N}.json");
var documentStore = new DocumentStore(tempStorePath, NullLogger<DocumentStore>.Instance);
var synonymService = new SynonymService(synonymsPath);
var queryBuilder = new QueryBuilder(synonymService);
var highlighter = new Highlighter();
var searchEngine = new SearchEngine(documentStore, queryBuilder, highlighter, NullLogger<SearchEngine>.Instance);
var ingestionService = new IngestionService(documentStore, NullLogger<IngestionService>.Instance);

// Ingest sources
var summary = ingestionService.IngestAll(projDocsPath, oshaXmlPath);
Console.WriteLine($"[Ingestion] Ingested {summary.IngestedCount} documents into store. Total available: {documentStore.Count}");
Console.WriteLine();

if (!File.Exists(casesPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: Could not find relevance cases file at {casesPath}");
    Console.ResetColor();
    return 1;
}

var json = File.ReadAllText(casesPath);
var casesDoc = JsonDocument.Parse(json);
var rootArray = casesDoc.RootElement.EnumerateArray();

int passed = 0;
int failed = 0;
int index = 1;

Console.WriteLine($"{"#",-3} | {"Query",-36} | {"As",-6} | {"Expected Condition",-35} | {"Result"}");
Console.WriteLine(new string('-', 95));

foreach (var item in rootArray)
{
    var query = item.GetProperty("query").GetString()!;
    var asUser = item.GetProperty("asUser").GetString()!;
    var expected = item.GetProperty("expected");
    var checkType = expected.GetProperty("type").GetString()!;

    var resp = searchEngine.Search(new SearchRequest
    {
        Q = query,
        AsUser = asUser,
        PageSize = 20
    });

    bool pass = false;
    string reason = string.Empty;
    string expectedDesc = string.Empty;

    var hitNumbers = resp.Hits.Select(h => h.Document.Number).ToList();

    switch (checkType)
    {
        case "is_first":
            {
                var docNum = expected.GetProperty("docNumber").GetString()!;
                expectedDesc = $"{docNum} is first";
                if (hitNumbers.Count > 0 && string.Equals(hitNumbers[0], docNum, StringComparison.OrdinalIgnoreCase))
                {
                    pass = true;
                    reason = $"Top hit: {hitNumbers[0]}";
                }
                else
                {
                    var actualFirst = hitNumbers.Count > 0 ? hitNumbers[0] : "None";
                    reason = $"Expected {docNum} first, got {actualFirst}";
                }
                break;
            }

        case "in_top_n":
            {
                var docNum = expected.GetProperty("docNumber").GetString()!;
                var n = expected.GetProperty("n").GetInt32();
                expectedDesc = $"{docNum} in top {n}";
                var topN = hitNumbers.Take(n).ToList();
                int rank = topN.FindIndex(num => string.Equals(num, docNum, StringComparison.OrdinalIgnoreCase));
                if (rank >= 0)
                {
                    pass = true;
                    reason = $"Found at rank #{rank + 1}";
                }
                else
                {
                    reason = $"Not in top {n} (Hits: {string.Join(", ", hitNumbers.Take(5))})";
                }
                break;
            }

        case "ranks_above":
            {
                var higher = expected.GetProperty("higherDocNumber").GetString()!;
                var lower = expected.GetProperty("lowerDocNumber").GetString()!;
                expectedDesc = $"{higher} ranks above {lower}";
                int rankHigh = hitNumbers.FindIndex(num => string.Equals(num, higher, StringComparison.OrdinalIgnoreCase));
                int rankLow = hitNumbers.FindIndex(num => string.Equals(num, lower, StringComparison.OrdinalIgnoreCase));

                if (rankHigh >= 0 && (rankLow < 0 || rankHigh < rankLow))
                {
                    pass = true;
                    reason = $"{higher} (rank #{rankHigh + 1}) > {lower} (rank #{rankLow + 1})";
                }
                else
                {
                    reason = $"Failed: {higher} rank={rankHigh + 1}, {lower} rank={rankLow + 1}";
                }
                break;
            }

        case "both_returned":
            {
                var docNums = expected.GetProperty("docNumbers").EnumerateArray().Select(e => e.GetString()!).ToList();
                expectedDesc = $"{string.Join(" & ", docNums)} both returned";
                var hasAll = docNums.All(d => hitNumbers.Any(h => string.Equals(h, d, StringComparison.OrdinalIgnoreCase)));
                if (hasAll)
                {
                    pass = true;
                    reason = "Both returned in hits";
                }
                else
                {
                    var missing = docNums.Where(d => !hitNumbers.Any(h => string.Equals(h, d, StringComparison.OrdinalIgnoreCase)));
                    reason = $"Missing: {string.Join(", ", missing)}";
                }
                break;
            }

        case "returned_and_not_returned":
            {
                var returnedNum = expected.GetProperty("returnedDocNumber").GetString()!;
                var notReturnedNum = expected.GetProperty("notReturnedDocNumber").GetString()!;
                expectedDesc = $"{returnedNum} in, {notReturnedNum} not";
                bool hasReturned = hitNumbers.Any(h => string.Equals(h, returnedNum, StringComparison.OrdinalIgnoreCase));
                bool hasNotReturned = hitNumbers.Any(h => string.Equals(h, notReturnedNum, StringComparison.OrdinalIgnoreCase));

                if (hasReturned && !hasNotReturned)
                {
                    pass = true;
                    reason = $"{returnedNum} included; {notReturnedNum} excluded";
                }
                else
                {
                    reason = $"hasReturned={hasReturned}, hasNotReturned={hasNotReturned}";
                }
                break;
            }

        case "not_returned_and_facet_only":
            {
                var forbiddenDoc = expected.GetProperty("docNumber").GetString()!;
                var allowedProjects = expected.GetProperty("expectedProjects").EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
                expectedDesc = $"{forbiddenDoc} not returned & facet P1 only";

                bool returnedForbidden = hitNumbers.Any(h => string.Equals(h, forbiddenDoc, StringComparison.OrdinalIgnoreCase));
                bool facetOnlyAllowed = resp.Facets.Projects.Keys.All(k => allowedProjects.Contains(k));

                if (!returnedForbidden && facetOnlyAllowed)
                {
                    pass = true;
                    reason = $"{forbiddenDoc} excluded; Facets=[{string.Join(", ", resp.Facets.Projects.Keys)}]";
                }
                else
                {
                    reason = $"returnedForbidden={returnedForbidden}, leakingFacets=[{string.Join(", ", resp.Facets.Projects.Keys)}]";
                }
                break;
            }
    }

    if (pass)
    {
        passed++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{index,2}. | {query,-36} | {asUser,-6} | {expectedDesc,-35} | [PASS] {reason}");
        Console.ResetColor();
    }
    else
    {
        failed++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{index,2}. | {query,-36} | {asUser,-6} | {expectedDesc,-35} | [FAIL] {reason}");
        Console.ResetColor();
    }
    index++;
}

Console.WriteLine(new string('-', 95));
Console.WriteLine($"Summary: {passed} passed, {failed} failed out of {passed + failed} relevance checks.");
if (failed == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("ALL RELEVANCE CHECKS PASSED!");
    Console.ResetColor();
    return 0;
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("SOME RELEVANCE CHECKS FAILED.");
    Console.ResetColor();
    return 1;
}
