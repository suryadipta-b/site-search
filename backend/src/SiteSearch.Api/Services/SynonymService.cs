using System.Text.Json;
using SiteSearch.Api.Models;

namespace SiteSearch.Api.Services;

public interface ISynonymService
{
    IReadOnlyList<string> ExpandPhrase(string text);
    IReadOnlyDictionary<string, List<string>> GetExpansions();
}

public class SynonymService : ISynonymService
{
    // Normalized lower-case phrase -> list of expansion phrases/words
    private readonly Dictionary<string, List<string>> _expansions = new(StringComparer.OrdinalIgnoreCase);

    public SynonymService(string configPath)
    {
        LoadConfig(configPath);
    }

    public SynonymService(IEnumerable<SynonymRule> rules)
    {
        BuildGraph(rules);
    }

    private void LoadConfig(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var json = File.ReadAllText(path);
        var rules = JsonSerializer.Deserialize<List<SynonymRule>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<SynonymRule>();

        BuildGraph(rules);
    }

    private void BuildGraph(IEnumerable<SynonymRule> rules)
    {
        _expansions.Clear();

        foreach (var rule in rules)
        {
            var source = rule.Term.Trim().ToLowerInvariant();
            var targets = rule.ExpandsTo.Select(t => t.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();

            if (rule.Direction.Equals("two-way", StringComparison.OrdinalIgnoreCase))
            {
                // In a two-way synonym rule, all items in the group are mutually equivalent.
                var allInGroup = new HashSet<string>(targets, StringComparer.OrdinalIgnoreCase) { source };

                foreach (var member in allInGroup)
                {
                    if (!_expansions.TryGetValue(member, out var currentList))
                    {
                        currentList = new List<string>();
                        _expansions[member] = currentList;
                    }

                    foreach (var sibling in allInGroup)
                    {
                        if (!sibling.Equals(member, StringComparison.OrdinalIgnoreCase) && !currentList.Contains(sibling, StringComparer.OrdinalIgnoreCase))
                        {
                            currentList.Add(sibling);
                        }
                    }
                }
            }
            else // "one-way"
            {
                // Only source expands to targets. Targets do NOT expand back to source.
                if (!_expansions.TryGetValue(source, out var currentList))
                {
                    currentList = new List<string>();
                    _expansions[source] = currentList;
                }

                foreach (var target in targets)
                {
                    if (!currentList.Contains(target, StringComparer.OrdinalIgnoreCase))
                    {
                        currentList.Add(target);
                    }
                }
            }
        }
    }

    public IReadOnlyList<string> ExpandPhrase(string text)
    {
        var normalized = text.Trim().ToLowerInvariant();
        if (_expansions.TryGetValue(normalized, out var list))
        {
            return list;
        }
        return Array.Empty<string>();
    }

    public IReadOnlyDictionary<string, List<string>> GetExpansions() => _expansions;
}
