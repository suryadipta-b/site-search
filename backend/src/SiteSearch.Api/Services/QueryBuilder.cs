using System.Text.RegularExpressions;
using SiteSearch.Api.Models;

namespace SiteSearch.Api.Services;

public interface IQueryBuilder
{
    ProcessedQuery BuildQuery(string? rawQuery);
    string Sanitize(string input);
    bool IsIdentifier(string token);
    bool IsFuzzyEligible(string token);
}

public class QueryBuilder : IQueryBuilder
{
    private readonly ISynonymService _synonymService;

    // Matches standard construction identifiers: RFI-0042, DL-P2-1107, NCR-0007, 1926.451, etc.
    private static readonly Regex IdentifierRegex = new(
        @"\b(?:(?:RFI|DL|NCR)(?:-[A-Za-z0-9]+)?-\d+|\d{4}\.\d+)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Engine syntax operators that could alter query engine syntax: + & | ! ( ) { } [ ] ^ " ~ * ? : \ /
    private static readonly Regex SyntaxCharactersRegex = new(
        @"[\+\&\|\!\(\)\{\}\[\]\^\""\~\*\?\:\\\/]",
        RegexOptions.Compiled);

    // Strip standalone hyphens (e.g. " -term" or trailing "-") while preserving hyphens between word characters like "RFI-0042"
    private static readonly Regex StandaloneHyphenRegex = new(
        @"(?<=\s)-(?=\w)|(?<=\w)-(?=\s)|^-(?=\w)|(?<=\w)-$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "for", "of", "a", "an", "the", "in", "on", "at", "to", "by", "is", "it"
    };

    public QueryBuilder(ISynonymService synonymService)
    {
        _synonymService = synonymService;
    }

    public string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Strip dangerous query syntax characters while keeping periods (in section numbers like 1926.451) and hyphens in identifiers
        var sanitized = SyntaxCharactersRegex.Replace(input, " ");
        sanitized = StandaloneHyphenRegex.Replace(sanitized, " ");
        // Normalize multiple spaces
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
        return sanitized;
    }

    public bool IsIdentifier(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return IdentifierRegex.IsMatch(token.Trim());
    }

    public bool IsFuzzyEligible(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var trimmed = token.Trim();

        // 1. Identifiers are never fuzzed
        if (IsIdentifier(trimmed))
            return false;

        // 2. Very short tokens (< 4 chars) are never fuzzed
        if (trimmed.Length < 4)
            return false;

        // 3. Numbers / alphanumeric codes are not fuzzed
        if (trimmed.All(char.IsDigit) || (trimmed.Any(char.IsDigit) && trimmed.Any(char.IsLetter)))
            return false;

        return true;
    }

    public ProcessedQuery BuildQuery(string? rawQuery)
    {
        var result = new ProcessedQuery
        {
            RawQuery = rawQuery ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return result;
        }

        // 1. Detect Identifiers directly on the raw input first
        var identifierMatches = IdentifierRegex.Matches(rawQuery);
        foreach (Match match in identifierMatches)
        {
            var id = match.Value.Trim();
            if (!result.DetectedIdentifiers.Contains(id, StringComparer.OrdinalIgnoreCase))
            {
                result.DetectedIdentifiers.Add(id);
                result.ExactOnlyTokens.Add(id);
            }
        }

        var sanitized = Sanitize(rawQuery);
        result.SanitizedQuery = sanitized;

        // 2. Multi-word phrase synonym detection
        var normalizedQueryLower = sanitized.ToLowerInvariant();
        var expansions = _synonymService.GetExpansions();

        // Check multi-word synonym keys first
        foreach (var entry in expansions.Where(e => e.Key.Contains(' ')))
        {
            if (normalizedQueryLower.Contains(entry.Key))
            {
                foreach (var expansion in entry.Value)
                {
                    var expTokens = expansion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var expToken in expTokens)
                    {
                        var t = expToken.ToLowerInvariant();
                        if (!StopWords.Contains(t) && !result.SynonymExpandedTokens.Contains(t, StringComparer.OrdinalIgnoreCase))
                        {
                            result.SynonymExpandedTokens.Add(t);
                        }
                    }
                }
            }
        }

        // 3. Tokenize into individual primary tokens
        var rawTokens = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawToken in rawTokens)
        {
            var token = rawToken.Trim();
            var lower = token.ToLowerInvariant();

            if (!result.PrimaryTokens.Contains(lower, StringComparer.OrdinalIgnoreCase))
            {
                result.PrimaryTokens.Add(lower);
            }

            // Check single-word synonym expansions
            var singleWordExpansions = _synonymService.ExpandPhrase(token);
            foreach (var expansion in singleWordExpansions)
            {
                var words = expansion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var w = word.ToLowerInvariant();
                    if (!StopWords.Contains(w) &&
                        !result.SynonymExpandedTokens.Contains(w, StringComparer.OrdinalIgnoreCase) &&
                        !result.PrimaryTokens.Contains(w, StringComparer.OrdinalIgnoreCase))
                    {
                        result.SynonymExpandedTokens.Add(w);
                    }
                }
            }

            // Check fuzziness eligibility
            if (IsFuzzyEligible(token))
            {
                result.FuzzyEligibleTokens.Add(lower);
            }
            else
            {
                result.ExactOnlyTokens.Add(lower);
            }
        }

        return result;
    }
}
