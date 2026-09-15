using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class FactualChangeOutboxRepositoryTests
{
    [Fact]
    public void BuildPendingFilter_ShouldMatchTheCompoundIndexPrefix()
    {
        FilterDefinition<FactualChangeOutboxDocument> filter =
            FactualChangeOutboxRepository.BuildPendingFilter(null);

        Assert.Equal(
            new BsonDocument("materializedAtUtc", BsonNull.Value),
            Render(filter));
    }

    [Fact]
    public void BuildPendingFilter_WithCursor_ShouldPageByDateThenIdentifier()
    {
        DateTime recordedAtUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        FactualChangeOutboxCursor cursor = new FactualChangeOutboxCursor(
            recordedAtUtc,
            "outbox-100");

        BsonDocument rendered = Render(
            FactualChangeOutboxRepository.BuildPendingFilter(cursor));
        string json = rendered.ToJson();

        Assert.Contains("$or", json, StringComparison.Ordinal);
        Assert.Contains("createdAt", json, StringComparison.Ordinal);
        Assert.Contains("outbox-100", json, StringComparison.Ordinal);
        Assert.Contains("_id", json, StringComparison.Ordinal);
    }

    private static BsonDocument Render(FilterDefinition<FactualChangeOutboxDocument> filter)
    {
        IBsonSerializer<FactualChangeOutboxDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<FactualChangeOutboxDocument>();
        RenderArgs<FactualChangeOutboxDocument> arguments =
            new RenderArgs<FactualChangeOutboxDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
