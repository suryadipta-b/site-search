using SiteSearch.Api.Models;
using SiteSearch.Api.Services;
using Xunit;

namespace SiteSearch.Tests;

public class QueryBuilderTests
{
    private readonly IQueryBuilder _queryBuilder;

    public QueryBuilderTests()
    {
        var rules = new List<SynonymRule>
        {
            new() { Term = "rebar", ExpandsTo = new() { "reinforcement bar", "reinforcing steel", "sariya" }, Direction = "two-way" },
            new() { Term = "formwork", ExpandsTo = new() { "shuttering", "centering" }, Direction = "two-way" },
            new() { Term = "GWB", ExpandsTo = new() { "gypsum wallboard", "gypsum board", "drywall" }, Direction = "two-way" },
            new() { Term = "RFI", ExpandsTo = new() { "request for information" }, Direction = "two-way" },
            new() { Term = "PPE", ExpandsTo = new() { "personal protective equipment" }, Direction = "two-way" },
            new() { Term = "fall protection", ExpandsTo = new() { "guardrail", "safety net", "personal fall arrest system" }, Direction = "one-way" }
        };

        var synonymService = new SynonymService(rules);
        _queryBuilder = new QueryBuilder(synonymService);
    }

    [Theory]
    [InlineData("RFI-0042", true)]
    [InlineData("1926.451", true)]
    [InlineData("DL-P2-1107", true)]
    [InlineData("NCR-0007", true)]
    [InlineData("concrete", false)]
    [InlineData("scaffold", false)]
    [InlineData("P1", false)]
    public void IsIdentifier_DetectsCorrectly(string token, bool expected)
    {
        var actual = _queryBuilder.IsIdentifier(token);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("RFI-0042", false)] // Identifiers not fuzzed
    [InlineData("1926.451", false)] // Identifiers not fuzzed
    [InlineData("P1", false)]       // Short tokens (<4) not fuzzed
    [InlineData("PT", false)]       // Short tokens (<4) not fuzzed
    [InlineData("M15", false)]      // Alphanumeric code not fuzzed
    [InlineData("scafold", true)]   // >= 4 chars regular word is fuzzy-eligible
    [InlineData("shuttering", true)]
    public void IsFuzzyEligible_AppliesRules(string token, bool expected)
    {
        var actual = _queryBuilder.IsFuzzyEligible(token);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildQuery_DetectsIdentifiersInComplexQuery()
    {
        var query = "Check status of RFI-0042 and OSHA 1926.451 on site";
        var processed = _queryBuilder.BuildQuery(query);

        Assert.Contains("RFI-0042", processed.DetectedIdentifiers);
        Assert.Contains("1926.451", processed.DetectedIdentifiers);
    }

    [Fact]
    public void BuildQuery_ExpandsTwoWaySynonymsBidirectionally()
    {
        // 1. Searching "sariya" expands to "rebar"
        var queryA = _queryBuilder.BuildQuery("sariya spacing slab");
        Assert.Contains("rebar", queryA.SynonymExpandedTokens);

        // 2. Searching "rebar" expands to "sariya"
        var queryB = _queryBuilder.BuildQuery("rebar spacing slab");
        Assert.Contains("sariya", queryB.SynonymExpandedTokens);

        // 3. Searching "drywall" expands to "gwb"
        var queryC = _queryBuilder.BuildQuery("drywall partition");
        Assert.Contains("gwb", queryC.SynonymExpandedTokens);
    }

    [Fact]
    public void BuildQuery_RespectsOneWaySynonymDirection()
    {
        // "fall protection" is a one-way synonym expanding to guardrail, safety net, personal fall arrest system
        var queryFall = _queryBuilder.BuildQuery("fall protection");
        Assert.Contains("guardrail", queryFall.SynonymExpandedTokens);

        // "guardrail" should NOT expand back to "fall protection"
        var queryGuard = _queryBuilder.BuildQuery("guardrail");
        Assert.DoesNotContain("fall", queryGuard.SynonymExpandedTokens);
        Assert.DoesNotContain("protection", queryGuard.SynonymExpandedTokens);
    }

    [Theory]
    [InlineData("scafold + guardrail && !danger")]
    [InlineData("title:\"test\" OR (body:slab AND NOT P2)")]
    [InlineData("rebar~1 [bracket] {brace} ^5 ? * \\")]
    public void Sanitize_StripsDangerousQuerySyntaxCharacters(string messyInput)
    {
        var sanitized = _queryBuilder.Sanitize(messyInput);

        Assert.DoesNotContain("+", sanitized);
        Assert.DoesNotContain("&", sanitized);
        Assert.DoesNotContain("!", sanitized);
        Assert.DoesNotContain("(", sanitized);
        Assert.DoesNotContain(")", sanitized);
        Assert.DoesNotContain("{", sanitized);
        Assert.DoesNotContain("}", sanitized);
        Assert.DoesNotContain("^", sanitized);
        Assert.DoesNotContain("~", sanitized);
        Assert.DoesNotContain("*", sanitized);
        Assert.DoesNotContain("?", sanitized);
        Assert.DoesNotContain("\\", sanitized);
    }
}
