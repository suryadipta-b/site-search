using System.Text.RegularExpressions;
using SiteSearch.Api.Models;
using SiteSearch.Api.Storage;

namespace SiteSearch.Api.Services;

public interface ISearchEngine
{
    SearchResponse Search(SearchRequest request);
}

public class SearchEngine : ISearchEngine
{
    private readonly IDocumentStore _documentStore;
    private readonly IQueryBuilder _queryBuilder;
    private readonly IHighlighter _highlighter;
    private readonly ILogger<SearchEngine> _logger;

    public SearchEngine(
        IDocumentStore documentStore,
        IQueryBuilder queryBuilder,
        IHighlighter highlighter,
        ILogger<SearchEngine> logger)
    {
        _documentStore = documentStore;
        _queryBuilder = queryBuilder;
        _highlighter = highlighter;
        _logger = logger;
    }

    public SearchResponse Search(SearchRequest request)
    {
        var user = HardcodedUsers.GetUserOrDefault(request.AsUser);
        var processed = _queryBuilder.BuildQuery(request.Q);

        // 1. Fetch all documents
        var allDocs = _documentStore.GetAll();

        // 2. Access Control Filtering inside the query:
        // Regulations (null ProjectId) visible to everyone.
        // Project documents visible ONLY to project members.
        var accessibleDocs = allDocs
            .Where(d => user.CanAccessProject(d.ProjectId))
            .ToList();

        // If query is empty, return accessible documents (or filtered by type/project)
        var isMatchAll = string.IsNullOrWhiteSpace(processed.SanitizedQuery);

        var scoredHits = new List<ScoredHit>();

        foreach (var doc in accessibleDocs)
        {
            if (isMatchAll)
            {
                scoredHits.Add(new ScoredHit(doc, 1.0, new List<string>()));
                continue;
            }

            var (score, matchedTokens) = ScoreDocument(doc, processed);
            if (score > 0)
            {
                scoredHits.Add(new ScoredHit(doc, score, matchedTokens));
            }
        }

        // 3. Compute Facets on the accessible matching documents (BEFORE applying type/project filter)
        // This ensures facet counts NEVER leak inaccessible projects or documents
        var facetResult = new FacetResult();

        foreach (var hit in scoredHits)
        {
            // Type facet
            if (!string.IsNullOrWhiteSpace(hit.Doc.Type))
            {
                var typeKey = hit.Doc.Type;
                facetResult.Types[typeKey] = facetResult.Types.GetValueOrDefault(typeKey, 0) + 1;
            }

            // Project facet: only count if document has a project and user can access that project
            if (!string.IsNullOrWhiteSpace(hit.Doc.ProjectId) && user.CanAccessProject(hit.Doc.ProjectId))
            {
                var projKey = hit.Doc.ProjectId;
                facetResult.Projects[projKey] = facetResult.Projects.GetValueOrDefault(projKey, 0) + 1;
            }
        }

        // 4. Apply Type and Project filters
        var filteredHits = scoredHits.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            filteredHits = filteredHits.Where(h => h.Doc.Type.Equals(request.Type.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Project))
        {
            filteredHits = filteredHits.Where(h => string.Equals(h.Doc.ProjectId, request.Project.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // 5. Sort by Score descending, then by Date descending
        var sortedHits = filteredHits
            .OrderByDescending(h => h.Score)
            .ThenByDescending(h => h.Doc.CreatedAt)
            .ToList();

        var total = sortedHits.Count;

        // 6. Pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));
        var pagedItems = sortedHits
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 7. Generate snippets
        var searchHits = new List<SearchHit>();
        foreach (var item in pagedItems)
        {
            var snippet = _highlighter.GenerateSnippet(item.Doc.Title, item.Doc.Body, item.MatchedTokens);
            searchHits.Add(new SearchHit
            {
                Document = item.Doc,
                Score = Math.Round(item.Score, 3),
                Snippet = snippet,
                MatchedTokens = item.MatchedTokens
            });
        }

        return new SearchResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Hits = searchHits,
            Facets = facetResult,
            ActiveUser = user.Username
        };
    }

    private (double score, List<string> matchedTokens) ScoreDocument(Document doc, ProcessedQuery query)
    {
        double score = 0.0;
        var matchedTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var docNumber = doc.Number.Trim();
        var docTitle = doc.Title.Trim();
        var docBody = doc.Body.Trim();
        var docType = doc.Type.Trim();
        var docDiscipline = doc.Discipline ?? string.Empty;

        var titleWords = Tokenize(docTitle);
        var bodyWords = Tokenize(docBody);
        var numberWords = Tokenize(docNumber);

        // A. Exact Identifier Boost (+100)
        // If user typed an identifier (e.g. RFI-0042, 1926.451, RFI-0044) and it exactly matches doc.Number:
        foreach (var id in query.DetectedIdentifiers)
        {
            if (string.Equals(docNumber, id, StringComparison.OrdinalIgnoreCase))
            {
                score += 100.0;
                matchedTokens.Add(docNumber);
            }
            else if (docNumber.Contains(id, StringComparison.OrdinalIgnoreCase))
            {
                score += 40.0;
                matchedTokens.Add(id);
            }
            else if (docBody.Contains(id, StringComparison.OrdinalIgnoreCase) || docTitle.Contains(id, StringComparison.OrdinalIgnoreCase))
            {
                // Document merely mentions the identifier in the body (e.g. DL-P2-1107 mentions RFI-0044)
                score += 5.0;
                matchedTokens.Add(id);
            }
        }

        // B. Primary Tokens Scoring
        int primaryMatches = 0;
        foreach (var token in query.PrimaryTokens)
        {
            var termScore = 0.0;

            // 1. Exact match in Number (Weight: 20)
            if (numberWords.Any(w => w.Equals(token, StringComparison.OrdinalIgnoreCase)) ||
                docNumber.Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                termScore += 20.0;
                matchedTokens.Add(token);
            }

            // 2. Exact match in Title (Weight: 10)
            if (titleWords.Any(w => w.Equals(token, StringComparison.OrdinalIgnoreCase)))
            {
                termScore += 10.0;
                matchedTokens.Add(token);
            }
            else if (docTitle.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                termScore += 6.0;
                matchedTokens.Add(token);
            }

            // 3. Exact match in Body (Weight: 2)
            var bodyOccurrences = bodyWords.Count(w => w.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (bodyOccurrences > 0)
            {
                termScore += Math.Min(10.0, 2.0 * bodyOccurrences);
                matchedTokens.Add(token);
            }
            else if (docBody.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                termScore += 1.5;
                matchedTokens.Add(token);
            }

            // 4. Exact match in Discipline or Type (Weight: 3)
            if (docDiscipline.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                docType.Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                termScore += 3.0;
                matchedTokens.Add(token);
            }

            // 5. Typo Tolerance / Fuzzy Match (if eligible and no exact match found yet)
            if (termScore == 0 && query.FuzzyEligibleTokens.Contains(token))
            {
                // Check title words
                var fuzzyTitleMatch = titleWords.FirstOrDefault(w => w.Length >= 4 && IsFuzzyMatch(token, w));
                if (fuzzyTitleMatch != null)
                {
                    termScore += 6.0; // Slightly discounted from exact title match
                    matchedTokens.Add(fuzzyTitleMatch);
                }
                else
                {
                    // Check body words
                    var fuzzyBodyMatch = bodyWords.FirstOrDefault(w => w.Length >= 4 && IsFuzzyMatch(token, w));
                    if (fuzzyBodyMatch != null)
                    {
                        termScore += 1.5;
                        matchedTokens.Add(fuzzyBodyMatch);
                    }
                }
            }

            if (termScore > 0)
            {
                score += termScore;
                primaryMatches++;
            }
        }

        // C. Synonym Tokens Scoring
        foreach (var synToken in query.SynonymExpandedTokens)
        {
            var synScore = 0.0;

            // Synonym match in Title (Weight: 7)
            if (titleWords.Any(w => w.Equals(synToken, StringComparison.OrdinalIgnoreCase)))
            {
                synScore += 7.0;
                matchedTokens.Add(synToken);
            }
            else if (docTitle.Contains(synToken, StringComparison.OrdinalIgnoreCase))
            {
                synScore += 4.5;
                matchedTokens.Add(synToken);
            }

            // Synonym match in Body (Weight: 1.5)
            var synBodyCount = bodyWords.Count(w => w.Equals(synToken, StringComparison.OrdinalIgnoreCase));
            if (synBodyCount > 0)
            {
                synScore += Math.Min(6.0, 1.5 * synBodyCount);
                matchedTokens.Add(synToken);
            }
            else if (docBody.Contains(synToken, StringComparison.OrdinalIgnoreCase))
            {
                synScore += 1.0;
                matchedTokens.Add(synToken);
            }

            score += synScore;
        }

        // Coordination factor: reward documents that match a higher fraction of primary query terms
        if (query.PrimaryTokens.Count > 1 && primaryMatches > 1)
        {
            double coordinationFactor = (double)primaryMatches / query.PrimaryTokens.Count;
            score *= (1.0 + coordinationFactor);
        }

        return (score, matchedTokens.ToList());
    }

    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return Regex.Split(text, @"[^\p{L}\p{Nd}\.\-]+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .ToList();
    }

    private static bool IsFuzzyMatch(string queryTerm, string candidate)
    {
        int dist = DamerauLevenshteinDistance(queryTerm, candidate);
        // Distance 1 for terms of length >= 4
        return dist <= 1;
    }

    public static int DamerauLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        int sLen = source.Length;
        int tLen = target.Length;

        int[,] d = new int[sLen + 1, tLen + 1];

        for (int i = 0; i <= sLen; i++) d[i, 0] = i;
        for (int j = 0; j <= tLen; j++) d[0, j] = j;

        for (int i = 1; i <= sLen; i++)
        {
            for (int j = 1; j <= tLen; j++)
            {
                int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );

                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                }
            }
        }

        return d[sLen, tLen];
    }

    private record ScoredHit(Document Doc, double Score, List<string> MatchedTokens);
}
