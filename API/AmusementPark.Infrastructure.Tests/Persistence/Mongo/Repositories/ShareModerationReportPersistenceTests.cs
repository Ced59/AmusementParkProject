using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ShareModerationReportPersistenceTests
{
    private static readonly DateTime SubmittedAtUtc =
        new DateTime(2026, 9, 13, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Mapper_ShouldRoundTripReviewedReportWithoutPublicShareToken()
    {
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-1"),
            ShareModerationTargetType.PassportProfile,
            "publication-1",
            ShareModerationReason.Impersonation,
            "This profile uses my name.",
            SubmittedAtUtc);
        report.MarkPublicationSuspended(
            "admin-1",
            "Identity claim requires review.",
            SubmittedAtUtc.AddMinutes(2));

        ShareModerationReportDocument document = report.ToDocument();
        ShareModerationReport restored = document.ToDomain();
        BsonDocument bson = document.ToBsonDocument();

        Assert.Equal(report.Id, restored.Id);
        Assert.Equal(report.TargetRecordId, restored.TargetRecordId);
        Assert.Equal(report.Status, restored.Status);
        Assert.Equal(report.Version, restored.Version);
        Assert.Equal(report.DecisionNote, restored.DecisionNote);
        Assert.DoesNotContain(
            bson.Names,
            static name => name.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Definitions_ShouldCreateStableQueueAndTargetHistoryIndexes()
    {
        IReadOnlyCollection<CreateIndexModel<ShareModerationReportDocument>> indexes =
            ShareModerationReportMongoDefinitions.BuildIndexes();

        Assert.Equal(2, indexes.Count);
        CreateIndexModel<ShareModerationReportDocument> queue = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ShareModerationReportMongoDefinitions.StatusQueueIndexName);
        Assert.Equal(
            new BsonDocument
            {
                { "status", 1 },
                { "submittedAtUtc", -1 },
                { "_id", -1 },
            },
            Render(queue.Keys));
        CreateIndexModel<ShareModerationReportDocument> history = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ShareModerationReportMongoDefinitions.TargetHistoryIndexName);
        Assert.Equal(
            new BsonDocument
            {
                { "targetType", 1 },
                { "targetRecordId", 1 },
                { "submittedAtUtc", -1 },
            },
            Render(history.Keys));
    }

    private static BsonDocument Render(
        IndexKeysDefinition<ShareModerationReportDocument> keys)
    {
        IBsonSerializer<ShareModerationReportDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ShareModerationReportDocument>();
        return keys.Render(new RenderArgs<ShareModerationReportDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
