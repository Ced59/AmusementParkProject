using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class HistoricalNarrativeCollectionCutoverMigrationTests
{
    [Fact]
    public void PrepareNarrative_ShouldKeepContentAndRequireCanonicalReview()
    {
        BsonDocument source = new BsonDocument
        {
            { "_id", "event-1" },
            { "ownerId", "park-1" },
            { "migrationVersion", "hist-04-history-events-v1" },
            { "migrationWarnings", new BsonArray { "old-warning" } },
            { "canonicalizationState", "Canonicalized" },
        };

        BsonDocument narrative = HistoricalNarrativeCollectionCutoverMigration.PrepareNarrative(
            source);

        Assert.Equal("event-1", narrative["_id"].AsString);
        Assert.Equal("park-1", narrative["ownerId"].AsString);
        Assert.Equal(
            HistoricalNarrativeCanonicalizationState.PendingReview.ToString(),
            narrative["canonicalizationState"].AsString);
        Assert.False(narrative.Contains("migrationVersion"));
        Assert.False(narrative.Contains("migrationWarnings"));
        Assert.True(source.Contains("migrationVersion"));
    }

    [Fact]
    public void PrepareNarrative_WhenIdentifierIsMissing_ShouldRejectDocument()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            HistoricalNarrativeCollectionCutoverMigration.PrepareNarrative(
                new BsonDocument("ownerId", "park-1")));

        Assert.Contains("identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
