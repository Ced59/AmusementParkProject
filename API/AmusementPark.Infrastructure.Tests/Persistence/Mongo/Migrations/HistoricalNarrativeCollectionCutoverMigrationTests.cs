using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
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
            { "canonicalFactId", "fact-before-cutover" },
        };

        BsonDocument narrative = HistoricalNarrativeCollectionCutoverMigration.PrepareNarrative(
            source);

        Assert.Equal("event-1", narrative["_id"].AsString);
        Assert.Equal("park-1", narrative["ownerId"].AsString);
        Assert.Equal(
            HistoricalNarrativeCanonicalizationState.PendingReview.ToString(),
            narrative["canonicalizationState"].AsString);
        Assert.Equal(
            HistoricalNarrativeCollectionCutoverMigration.CutoverVersion,
            narrative["cutoverVersion"].AsString);
        Assert.Equal(
            "fact-before-cutover",
            narrative[HistoricalNarrativeCollectionCutoverMigration.PreviousCanonicalFactIdField]
                .AsString);
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

    [Fact]
    public void PrepareNarrative_WhenNoPreviousFactExists_ShouldNotInventRollbackReference()
    {
        BsonDocument narrative = HistoricalNarrativeCollectionCutoverMigration.PrepareNarrative(
            new BsonDocument
            {
                { "_id", "event-without-fact" },
                { "ownerId", "park-1" },
            });

        Assert.False(narrative.Contains(
            HistoricalNarrativeCollectionCutoverMigration.PreviousCanonicalFactIdField));
    }

    [Fact]
    public void BuildAffectedFactFilter_ShouldProtectEveryFactFamilyMatchedByRollback()
    {
        FilterDefinition<BsonDocument> filter =
            HistoricalNarrativeCollectionCutoverMigration.BuildAffectedFactFilter(
                new[] { "narrative-1" },
                new[] { "fact-1" });

        BsonDocument rendered = filter.Render(new RenderArgs<BsonDocument>(
            BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal("narrative-1", rendered["$or"][0]["narrativeContentId"]["$in"][0].AsString);
        Assert.Equal("fact-1", rendered["$or"][1]["factId"]["$in"][0].AsString);
    }
}
