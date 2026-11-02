namespace SiteSearch.Api.Models;

public class ProcessedQuery
{
    public string RawQuery { get; set; } = string.Empty;
    public string SanitizedQuery { get; set; } = string.Empty;
    public List<string> DetectedIdentifiers { get; set; } = new();
    public List<string> PrimaryTokens { get; set; } = new();
    public List<string> SynonymExpandedTokens { get; set; } = new();
    public HashSet<string> FuzzyEligibleTokens { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExactOnlyTokens { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
