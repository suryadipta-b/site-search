using System.Text.Json.Serialization;

namespace SiteSearch.Api.Models;

public class SearchRequest
{
    [JsonPropertyName("q")]
    public string? Q { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("project")]
    public string? Project { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("as")]
    public string? AsUser { get; set; }
}

public class SearchResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("hits")]
    public List<SearchHit> Hits { get; set; } = new();

    [JsonPropertyName("facets")]
    public FacetResult Facets { get; set; } = new();

    [JsonPropertyName("activeUser")]
    public string ActiveUser { get; set; } = "guest";
}

public class SearchHit
{
    [JsonPropertyName("document")]
    public Document Document { get; set; } = new();

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;

    [JsonPropertyName("matchedTokens")]
    public List<string> MatchedTokens { get; set; } = new();
}

public class FacetResult
{
    [JsonPropertyName("types")]
    public Dictionary<string, int> Types { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("projects")]
    public Dictionary<string, int> Projects { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
