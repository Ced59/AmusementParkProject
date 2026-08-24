using System.Text.RegularExpressions;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class StandaloneAttractionPublicFilterTests
{
    [Theory]
    [InlineData("ClosedDefinitively")]
    [InlineData("closed-definitively")]
    [InlineData("permanently_closed")]
    [InlineData("definitively closed")]
    [InlineData("ferme definitivement")]
    [InlineData("fermé définitivement")]
    [InlineData("fermé-définitivement")]
    [InlineData("fermé'définitivement")]
    public void BuildPublicFilter_WhenStatusIsDefinitivelyClosed_ShouldExcludeStatus(string status)
    {
        Regex closedStatusExpression = GetClosedStatusExpression();

        Assert.True(ParkItemStatusNormalizer.IsClosedDefinitively(status));
        Assert.Matches(closedStatusExpression, status);
    }

    [Fact]
    public void BuildPublicFilter_WhenStatusIsOperating_ShouldKeepStatus()
    {
        Regex closedStatusExpression = GetClosedStatusExpression();

        Assert.DoesNotMatch(closedStatusExpression, ParkItemStatusNormalizer.Operating);
    }

    private static Regex GetClosedStatusExpression()
    {
        FilterDefinition<StandaloneAttractionDocument> filter = StandaloneAttractionRepository.BuildPublicFilter();
        IBsonSerializer<StandaloneAttractionDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<StandaloneAttractionDocument>();
        BsonDocument renderedFilter = filter.Render(new RenderArgs<StandaloneAttractionDocument>(serializer, BsonSerializer.SerializerRegistry));
        BsonRegularExpression expression = renderedFilter["attractionDetails.status"].AsBsonDocument["$not"].AsBsonRegularExpression;

        return new Regex(expression.Pattern, RegexOptions.IgnoreCase);
    }
}
