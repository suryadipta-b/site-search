using System.Text.Json.Serialization;

namespace SiteSearch.Api.Models;

public class Document
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; } // null = global (regulations)

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Regulation | RFI | DailyLog | NCR

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty; // e.g. RFI-0042, 1926.451

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("discipline")]
    public string? Discipline { get; set; } // Civil | Structural | Architectural | MEP | HSE

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("subpart")]
    public string? Subpart { get; set; } // Retained for display for regulations (e.g., "Subpart L—Scaffolds")
}
