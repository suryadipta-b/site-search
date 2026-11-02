using System.Text.Json.Serialization;

namespace SiteSearch.Api.Models;

public class SynonymRule
{
    [JsonPropertyName("term")]
    public string Term { get; set; } = string.Empty;

    [JsonPropertyName("expandsTo")]
    public List<string> ExpandsTo { get; set; } = new();

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "two-way"; // "two-way" or "one-way"
}
