using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkItemClosedEntityMongoFilterTests
{
    [Theory]
    [InlineData("TemporarilyClosed")]
    [InlineData("temporarily closed")]
    [InlineData("ClosedDefinitively")]
    [InlineData("permanently closed")]
    [InlineData("Removed")]
    [InlineData("dismantled")]
    public void Build_WhenClosedOnly_ShouldMatchEveryClosedVisitorState(string status)
    {
        BsonDocument filter = Render(ParkItemClosedEntityMongoFilter.Build(ClosedEntityFilter.ClosedOnly));
        BsonRegularExpression expression = filter["attractionDetails.status"].AsBsonRegularExpression;

        Assert.Matches(new Regex(expression.Pattern, RegexOptions.IgnoreCase), status);
    }

    [Theory]
    [InlineData("Operating")]
    [InlineData("Planned")]
    [InlineData("UnderConstruction")]
    [InlineData("Unknown")]
    public void Build_WhenClosedOnly_ShouldExcludeStatesThatAreNotClosed(string status)
    {
        BsonDocument filter = Render(ParkItemClosedEntityMongoFilter.Build(ClosedEntityFilter.ClosedOnly));
        BsonRegularExpression expression = filter["attractionDetails.status"].AsBsonRegularExpression;

        Assert.DoesNotMatch(new Regex(expression.Pattern, RegexOptions.IgnoreCase), status);
    }

    [Fact]
    public void Build_WhenOpenOnly_ShouldNegateTheSharedClosedStatusPattern()
    {
        BsonDocument filter = Render(ParkItemClosedEntityMongoFilter.Build(ClosedEntityFilter.OpenOnly));

        Assert.True(filter["attractionDetails.status"].AsBsonDocument.Contains("$not"));
    }

    [Fact]
    public void Build_WhenAll_ShouldNotFilterLifecycleStatus()
    {
        BsonDocument filter = Render(ParkItemClosedEntityMongoFilter.Build(ClosedEntityFilter.All));

        Assert.Empty(filter);
    }

    private static BsonDocument Render(FilterDefinition<ParkItemDocument> filter)
    {
        IBsonSerializer<ParkItemDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<ParkItemDocument>();
        return filter.Render(new RenderArgs<ParkItemDocument>(serializer, BsonSerializer.SerializerRegistry));
    }
}
