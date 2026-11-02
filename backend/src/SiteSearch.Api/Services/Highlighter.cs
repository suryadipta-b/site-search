using System.Net;
using System.Text.RegularExpressions;

namespace SiteSearch.Api.Services;

public interface IHighlighter
{
    string GenerateSnippet(string title, string body, IEnumerable<string> searchTokens, int maxChars = 200);
}

public class Highlighter : IHighlighter
{
    public string GenerateSnippet(string title, string body, IEnumerable<string> searchTokens, int maxChars = 200)
    {
        var tokens = searchTokens
            .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length >= 2)
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .OrderByDescending(t => t.Length)
            .ToList();

        var text = string.IsNullOrWhiteSpace(body) ? title : body;
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (tokens.Count == 0)
        {
            var truncated = text.Length <= maxChars ? text : text[..maxChars] + "...";
            return WebUtility.HtmlEncode(truncated);
        }

        // Find best window in text that contains the most token matches
        int bestStart = 0;
        int maxHits = -1;

        // Break text into sentences or words to find the best window
        var lowerText = text.ToLowerInvariant();
        int firstMatchIndex = -1;

        foreach (var token in tokens)
        {
            var idx = lowerText.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && (firstMatchIndex == -1 || idx < firstMatchIndex))
            {
                firstMatchIndex = idx;
            }
        }

        if (firstMatchIndex >= 0)
        {
            bestStart = Math.Max(0, firstMatchIndex - 40);
            // Move back to word boundary
            while (bestStart > 0 && !char.IsWhiteSpace(text[bestStart]))
            {
                bestStart--;
            }
            if (char.IsWhiteSpace(text[bestStart])) bestStart++;
        }

        int length = Math.Min(maxChars, text.Length - bestStart);
        var snippetRaw = text.Substring(bestStart, length);

        if (bestStart > 0)
            snippetRaw = "..." + snippetRaw;
        if (bestStart + length < text.Length)
            snippetRaw = snippetRaw + "...";

        // HTML encode the snippet text first to ensure safety against XSS
        var safeSnippet = WebUtility.HtmlEncode(snippetRaw);

        // Highlight matched tokens safely using <mark>
        foreach (var token in tokens)
        {
            var escapedToken = WebUtility.HtmlEncode(token);
            var pattern = $@"\b({Regex.Escape(escapedToken)})\b";
            safeSnippet = Regex.Replace(safeSnippet, pattern, "<mark>$1</mark>", RegexOptions.IgnoreCase);
        }

        return safeSnippet;
    }
}
