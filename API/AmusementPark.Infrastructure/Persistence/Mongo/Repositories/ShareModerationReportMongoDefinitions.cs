using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ShareModerationReportMongoDefinitions
{
    public const string StatusQueueIndexName = "idx_share_moderation_status_submitted";
    public const string TargetHistoryIndexName = "idx_share_moderation_target_submitted";

    public static FilterDefinition<ShareModerationReportDocument> BuildIdFilter(string id)
    {
        return Builders<ShareModerationReportDocument>.Filter.Eq(
            static document => document.Id,
            id);
    }

    public static FilterDefinition<ShareModerationReportDocument> BuildVersionFilter(
        string id,
        long version)
    {
        return BuildIdFilter(id)
            & Builders<ShareModerationReportDocument>.Filter.Eq(
                static document => document.Version,
                version);
    }

    public static FilterDefinition<ShareModerationReportDocument> BuildSearchFilter(
        ShareModerationReportStatus? status,
        ShareModerationTargetType? targetType,
        ShareModerationReason? reason)
    {
        FilterDefinitionBuilder<ShareModerationReportDocument> builder =
            Builders<ShareModerationReportDocument>.Filter;
        List<FilterDefinition<ShareModerationReportDocument>> filters = new();
        if (status.HasValue)
        {
            filters.Add(builder.Eq(static document => document.Status, status.Value));
        }

        if (targetType.HasValue)
        {
            filters.Add(builder.Eq(static document => document.TargetType, targetType.Value));
        }

        if (reason.HasValue)
        {
            filters.Add(builder.Eq(static document => document.Reason, reason.Value));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    public static IReadOnlyCollection<CreateIndexModel<ShareModerationReportDocument>> BuildIndexes()
    {
        CreateIndexModel<ShareModerationReportDocument> statusQueue = new(
            Builders<ShareModerationReportDocument>.IndexKeys
                .Ascending(static document => document.Status)
                .Descending(static document => document.SubmittedAtUtc)
                .Descending(static document => document.Id),
            new CreateIndexOptions { Name = StatusQueueIndexName });
        CreateIndexModel<ShareModerationReportDocument> targetHistory = new(
            Builders<ShareModerationReportDocument>.IndexKeys
                .Ascending(static document => document.TargetType)
                .Ascending(static document => document.TargetRecordId)
                .Descending(static document => document.SubmittedAtUtc),
            new CreateIndexOptions { Name = TargetHistoryIndexName });
        return new[] { statusQueue, targetHistory };
    }
}
