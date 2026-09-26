using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacyHistoryReplacementMigrationTests
{
    [Fact]
    public void EnsureSubjectSnapshotUnchanged_WhenVisibilityDigestChanges_ShouldBlockCutover()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => HistoricalLegacyHistoryReplacementMigration.EnsureSubjectSnapshotUnchanged(
                2,
                "before",
                2,
                "after"));

        Assert.Contains("subject changed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureSubjectSnapshotUnchanged_WhenSnapshotMatches_ShouldAllowCutover()
    {
        HistoricalLegacyHistoryReplacementMigration.EnsureSubjectSnapshotUnchanged(
            2,
            "same-digest",
            2,
            "same-digest");
    }

    [Fact]
    public void MigrationState_ShouldPersistTheSubjectSnapshotProof()
    {
        HistoricalLegacyMigrationStateDocument state = new HistoricalLegacyMigrationStateDocument
        {
            Id = HistoricalLegacyHistoryReplacementMigration.MigrationId,
            SubjectCount = 3,
            SubjectDigest = "subject-digest",
        };

        BsonDocument document = state.ToBsonDocument(
            BsonSerializer.SerializerRegistry.GetSerializer<HistoricalLegacyMigrationStateDocument>());

        Assert.Equal(3, document["subjectCount"].AsInt64);
        Assert.Equal("subject-digest", document["subjectDigest"].AsString);
    }
}
