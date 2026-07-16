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

    private static BsonDocument BuildSearchFilter(string text, IReadOnlyCollection<string> categories)
    {
        MethodInfo method = typeof(SearchReadRepository).GetMethod("BuildSearchFilter", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("SearchReadRepository.BuildSearchFilter was not found.");
        object? result = method.Invoke(null, new object[] { text, categories });
        return result as BsonDocument ?? throw new InvalidOperationException("SearchReadRepository.BuildSearchFilter did not return a BSON document.");
    }
}
