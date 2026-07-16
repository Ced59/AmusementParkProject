using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class SearchResultOrderingTests
{
    [Theory]
    [InlineData("parks", "park", 10)]
    [InlineData("parkItems", "attraction", 20)]
    [InlineData("standaloneAttractions", "standaloneAttraction", 25)]
    [InlineData("parkItems", "restaurant", 30)]
    [InlineData("parkItems", "service", 70)]
    [InlineData("manufacturers", "manufacturers", 100)]
    [InlineData("unknown", "unknown", 120)]
    public void ResolvePriority_ShouldOrderSearchResultsByPublicCategory(string resourceType, string category, int expectedPriority)
    {
        int priority = SearchResultOrdering.ResolvePriority(resourceType, category);

        Assert.Equal(expectedPriority, priority);
    }

    [Fact]
    public void BuildPriorityAddFieldsStage_ShouldExposeExpectedPipelineField()
    {
        string stage = SearchResultOrdering.BuildPriorityAddFieldsStage().ToJson();

        Assert.Contains(SearchResultOrdering.PriorityFieldName, stage, StringComparison.Ordinal);
        Assert.Contains("parkitems", stage, StringComparison.Ordinal);
        Assert.Contains("standaloneattractions", stage, StringComparison.Ordinal);
        Assert.Contains("restaurant", stage, StringComparison.Ordinal);
    }
}
