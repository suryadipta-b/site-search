using Microsoft.Extensions.Logging.Abstractions;
using SiteSearch.Api.Models;
using SiteSearch.Api.Services;
using SiteSearch.Api.Storage;
using Xunit;

namespace SiteSearch.Tests;

public class AccessControlTests
{
    private readonly ISearchEngine _searchEngine;
    private readonly IDocumentStore _documentStore;

    public AccessControlTests()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"store_test_{Guid.NewGuid():N}.json");
        _documentStore = new DocumentStore(tempFile, NullLogger<DocumentStore>.Instance);

        // Seed with test documents
        _documentStore.BulkUpsert(new List<Document>
        {
            new() { Id = "rfi-p1-0017", ProjectId = "P1", Type = "RFI", Number = "RFI-0017", Title = "Rebar spacing P1", Body = "Slab reinforcement" },
            new() { Id = "rfi-p2-0042", ProjectId = "P2", Type = "RFI", Number = "RFI-0042", Title = "Lap length P2", Body = "Transfer slab" },
            new() { Id = "reg-1926-451", ProjectId = null, Type = "Regulation", Number = "1926.451", Title = "Scaffolds", Body = "Guardrail requirement" }
        });

        var rules = new List<SynonymRule>();
        var synonymService = new SynonymService(rules);
        var queryBuilder = new QueryBuilder(synonymService);
        var highlighter = new Highlighter();

        _searchEngine = new SearchEngine(_documentStore, queryBuilder, highlighter, NullLogger<SearchEngine>.Instance);
    }

    [Fact]
    public void Guest_SeesOnlyGlobalRegulations_NoProjectDocuments()
    {
        var response = _searchEngine.Search(new SearchRequest { AsUser = "guest" });

        Assert.All(response.Hits, hit => Assert.Null(hit.Document.ProjectId));
        Assert.Contains(response.Hits, hit => hit.Document.Number == "1926.451");
        Assert.DoesNotContain(response.Hits, hit => hit.Document.ProjectId == "P1");
        Assert.DoesNotContain(response.Hits, hit => hit.Document.ProjectId == "P2");
    }

    [Fact]
    public void Asha_SeesOnlyP1AndRegulations_CannotAccessP2()
    {
        var response = _searchEngine.Search(new SearchRequest { AsUser = "asha" });

        Assert.Contains(response.Hits, hit => hit.Document.ProjectId == "P1");
        Assert.Contains(response.Hits, hit => hit.Document.ProjectId == null);
        Assert.DoesNotContain(response.Hits, hit => hit.Document.ProjectId == "P2");
    }

    [Fact]
    public void Ravi_SeesOnlyP2AndRegulations_CannotAccessP1()
    {
        var response = _searchEngine.Search(new SearchRequest { AsUser = "ravi" });

        Assert.Contains(response.Hits, hit => hit.Document.ProjectId == "P2");
        Assert.Contains(response.Hits, hit => hit.Document.ProjectId == null);
        Assert.DoesNotContain(response.Hits, hit => hit.Document.ProjectId == "P1");
    }

    [Fact]
    public void Meera_SeesBothP1AndP2Documents()
    {
        var response = _searchEngine.Search(new SearchRequest { AsUser = "meera" });

        Assert.Contains(response.Hits, hit => hit.Document.ProjectId == "P1");
        Assert.Contains(response.Hits, hit => hit.Document.ProjectId == "P2");
        Assert.Contains(response.Hits, hit => hit.Document.ProjectId == null);
    }

    [Fact]
    public void FacetCounts_DoNotLeakInaccessibleProjects()
    {
        // Asha searches for an identifier that exists in P2 (RFI-0042)
        var response = _searchEngine.Search(new SearchRequest { Q = "RFI-0042", AsUser = "asha" });

        // Document must NOT be returned
        Assert.Empty(response.Hits);

        // Project facet must NOT contain P2
        Assert.False(response.Facets.Projects.ContainsKey("P2"), "P2 leaked into Asha's project facets!");
    }
}

public class IngestionTests
{
    [Fact]
    public void Ingestion_IsIdempotent_RerunningCreatesNoDuplicates()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"store_ingest_{Guid.NewGuid():N}.json");
        var store = new DocumentStore(tempFile, NullLogger<DocumentStore>.Instance);
        var ingestion = new IngestionService(store, NullLogger<IngestionService>.Instance);

        var sampleJsonPath = Path.Combine(Path.GetTempPath(), $"sample_docs_{Guid.NewGuid():N}.json");
        File.WriteAllText(sampleJsonPath, @"[
            {""id"": ""doc-1"", ""projectId"": ""P1"", ""type"": ""RFI"", ""number"": ""RFI-01"", ""title"": ""Test 1"", ""body"": ""Body 1""},
            {""id"": ""doc-2"", ""projectId"": ""P2"", ""type"": ""NCR"", ""number"": ""NCR-01"", ""title"": ""Test 2"", ""body"": ""Body 2""}
        ]");

        // First run
        var res1 = ingestion.IngestProjectDocuments(sampleJsonPath);
        Assert.Equal(2, res1.IngestedCount);
        Assert.Equal(2, store.Count);

        // Second run with same file
        var res2 = ingestion.IngestProjectDocuments(sampleJsonPath);
        Assert.Equal(2, res2.IngestedCount);
        Assert.Equal(2, store.Count); // Total store count remains 2 (no duplicates!)
    }

    [Fact]
    public void Ingestion_SkipsMalformedRecords_WithLogging()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"store_malformed_{Guid.NewGuid():N}.json");
        var store = new DocumentStore(tempFile, NullLogger<DocumentStore>.Instance);
        var ingestion = new IngestionService(store, NullLogger<IngestionService>.Instance);

        var sampleJsonPath = Path.Combine(Path.GetTempPath(), $"malformed_{Guid.NewGuid():N}.json");
        File.WriteAllText(sampleJsonPath, @"[
            {""id"": """", ""projectId"": ""P1"", ""type"": ""RFI"", ""number"": ""RFI-01"", ""title"": ""Missing ID""},
            {""id"": ""doc-ok"", ""projectId"": ""P1"", ""type"": ""RFI"", ""number"": ""RFI-02"", ""title"": ""Valid Doc"", ""body"": ""Valid""}
        ]");

        var res = ingestion.IngestProjectDocuments(sampleJsonPath);
        Assert.Equal(1, res.IngestedCount);
        Assert.Equal(1, res.SkippedCount);
        Assert.Equal(1, store.Count);
        Assert.NotNull(store.GetById("doc-ok"));
    }
}
