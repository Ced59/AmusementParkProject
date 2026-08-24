using System.Reflection;
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
