using System.Reflection;
using System.Text.RegularExpressions;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class SearchReadRepositoryCategoryFilterTests
{
    [Fact]
    public void BuildSearchFilter_ForStandaloneAttractions_ShouldTargetStandaloneCategoryAndResourceType()
    {
        BsonDocument filter = BuildSearchFilter(string.Empty, new[] { "standaloneAttractions" });
        string json = filter.ToJson();

        Assert.Contains("standaloneAttraction", json, StringComparison.Ordinal);
        Assert.Contains("standaloneAttractions", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSearchFilter_ForAttractionsWithStandalone_ShouldIncludeParkAttractionsAndStandaloneAttractions()
    {
        BsonDocument filter = BuildSearchFilter(string.Empty, new[] { "attractionsWithStandalone" });
        string json = filter.ToJson();

        Assert.Contains("attraction", json, StringComparison.Ordinal);
        Assert.Contains("standaloneAttraction", json, StringComparison.Ordinal);
        Assert.DoesNotContain("parkItems", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSearchFilter_ForAttractions_ShouldNotBroadenToEveryParkItem()
    {
        BsonDocument filter = BuildSearchFilter(string.Empty, new[] { "attraction" });
        string json = filter.ToJson();

        Assert.Contains("attraction", json, StringComparison.Ordinal);
        Assert.DoesNotContain("parkItems", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSearchFilter_WithRegionCountryCodes_ShouldRestrictCountries()
    {
        BsonDocument filter = BuildSearchFilter(
            string.Empty,
            new[] { "park" },
            regionCountryCodes: new[] { "FR", "DE" });
        string json = filter.ToJson();

        Assert.Contains("countryCode", json, StringComparison.Ordinal);
        Assert.Contains("FR", json, StringComparison.Ordinal);
        Assert.Contains("DE", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSearchFilter_WithLocalizedCountryMatch_ShouldIncludeCountryAsTextAlternative()
    {
        BsonDocument filter = BuildSearchFilter(
            "Autriche",
            new[] { "standaloneAttractions" },
            matchingCountryCodes: new[] { "AT" });
        string json = filter.ToJson();

        Assert.Contains("Autriche", json, StringComparison.Ordinal);
        Assert.Contains("countryCode", json, StringComparison.Ordinal);
        Assert.Contains("AT", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("attraction isolée")]
    [InlineData("Standalone attractions only")]
    [InlineData("Nur eigenständige Attraktionen")]
    [InlineData("Alleen losse attracties")]
    [InlineData("Solo attrazioni isolate")]
    [InlineData("Solo atracciones aisladas")]
    [InlineData("Attractions isolées seules")]
    [InlineData("Tylko samodzielne atrakcje")]
    [InlineData("Só atrações isoladas")]
    public void BuildSearchFilter_WithLocalizedStandaloneLabelInMixedScope_ShouldKeepOriginalAndAddStandaloneAlias(string localizedLabel)
    {
        BsonDocument filter = BuildSearchFilter(localizedLabel, new[] { "park", "standaloneAttractions" });
        BsonArray clauses = filter["$and"].AsBsonArray;
        BsonDocument textClause = clauses
            .Select(value => value.AsBsonDocument)
            .Single(clause => clause.Contains("$or") && clause["$or"].AsBsonArray.Any(filterValue => filterValue.AsBsonDocument.Contains("title")));
        BsonArray textAlternatives = textClause["$or"].AsBsonArray;
        BsonRegularExpression originalTitleExpression = textAlternatives[0].AsBsonDocument["title"].AsBsonRegularExpression;
        BsonDocument standaloneAliasBranch = textAlternatives
            .Select(value => value.AsBsonDocument)
            .Single(alternative => alternative.Contains("$and"));
        BsonArray standaloneAliasClauses = standaloneAliasBranch["$and"].AsBsonArray;
        BsonRegularExpression canonicalTitleExpression = standaloneAliasClauses[1].AsBsonDocument["$or"].AsBsonArray[0].AsBsonDocument["title"].AsBsonRegularExpression;

        Assert.Equal($".*{Regex.Escape(localizedLabel)}.*", originalTitleExpression.Pattern);
        Assert.Equal($".*{Regex.Escape("standalone attraction")}.*", canonicalTitleExpression.Pattern);
        Assert.Contains("standaloneAttraction", standaloneAliasBranch.ToJson(), StringComparison.Ordinal);
        Assert.Contains("standaloneAttractions", standaloneAliasBranch.ToJson(), StringComparison.Ordinal);
    }

    private static BsonDocument BuildSearchFilter(
        string text,
        IReadOnlyCollection<string> categories,
        IReadOnlyCollection<string>? matchingCountryCodes = null,
        IReadOnlyCollection<string>? regionCountryCodes = null)
    {
        MethodInfo method = typeof(SearchReadRepository).GetMethod("BuildSearchFilter", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("SearchReadRepository.BuildSearchFilter was not found.");
        object? result = method.Invoke(null, new object[]
        {
            text,
            categories,
            matchingCountryCodes ?? Array.Empty<string>(),
            regionCountryCodes ?? Array.Empty<string>(),
        });
        return result as BsonDocument ?? throw new InvalidOperationException("SearchReadRepository.BuildSearchFilter did not return a BSON document.");
    }
}
