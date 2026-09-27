using AmusementPark.Application.Features.HistoricalExistenceReports.Models;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class HistoricalExistenceReportMongoDefinitions
{
    public const string StatusQueueIndexName = "idx_history_existence_status_submitted";
    public const string OwnerVisitIndexName = "idx_history_existence_owner_visit";
    public const string ParkStatusIndexName = "idx_history_existence_park_status";
    public const string PendingDuplicateIndexName = "ux_history_existence_pending_claim";

    public static FilterDefinition<HistoricalExistenceReportDocument> BuildIdFilter(string id)
    {
        return Builders<HistoricalExistenceReportDocument>.Filter.Eq(
            static document => document.Id,
            id);
    }

    public static FilterDefinition<HistoricalExistenceReportDocument> BuildRevisionFilter(
        string id,
        long revision)
    {
        return BuildIdFilter(id)
            & Builders<HistoricalExistenceReportDocument>.Filter.Eq(
                static document => document.Revision,
                revision);
    }

    public static FilterDefinition<HistoricalExistenceReportDocument> BuildOwnedVisitFilter(
        string ownerUserId,
        string visitId)
    {
        FilterDefinitionBuilder<HistoricalExistenceReportDocument> builder =
            Builders<HistoricalExistenceReportDocument>.Filter;
        return builder.Eq(static document => document.OwnerUserId, ownerUserId)
            & builder.Eq(static document => document.VisitId, visitId);
    }

    public static FilterDefinition<HistoricalExistenceReportDocument> BuildSearchFilter(
        HistoricalExistenceReportSearchCriteria criteria)
    {
        FilterDefinitionBuilder<HistoricalExistenceReportDocument> builder =
            Builders<HistoricalExistenceReportDocument>.Filter;
        List<FilterDefinition<HistoricalExistenceReportDocument>> filters = new();
        if (criteria.Status.HasValue)
        {
            filters.Add(builder.Eq(static document => document.Status, criteria.Status.Value));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ParkId))
        {
            filters.Add(builder.Eq(static document => document.ParkId, criteria.ParkId.Trim()));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    public static IReadOnlyCollection<CreateIndexModel<HistoricalExistenceReportDocument>>
        BuildIndexes()
    {
        IndexKeysDefinitionBuilder<HistoricalExistenceReportDocument> keys =
            Builders<HistoricalExistenceReportDocument>.IndexKeys;
        CreateIndexModel<HistoricalExistenceReportDocument> queue = new(
            keys.Ascending(static document => document.Status)
                .Descending(static document => document.SubmittedAtUtc)
                .Descending(static document => document.Id),
            new CreateIndexOptions { Name = StatusQueueIndexName });
        CreateIndexModel<HistoricalExistenceReportDocument> ownedVisit = new(
            keys.Ascending(static document => document.OwnerUserId)
                .Ascending(static document => document.VisitId)
                .Descending(static document => document.SubmittedAtUtc),
            new CreateIndexOptions { Name = OwnerVisitIndexName });
        CreateIndexModel<HistoricalExistenceReportDocument> parkQueue = new(
            keys.Ascending(static document => document.ParkId)
                .Ascending(static document => document.Status)
                .Descending(static document => document.SubmittedAtUtc),
            new CreateIndexOptions { Name = ParkStatusIndexName });
        CreateIndexModel<HistoricalExistenceReportDocument> duplicate = new(
            keys.Ascending(static document => document.OwnerUserId)
                .Ascending(static document => document.VisitId)
                .Ascending(static document => document.NormalizedClaimedName),
            new CreateIndexOptions<HistoricalExistenceReportDocument>
            {
                Name = PendingDuplicateIndexName,
                Unique = true,
                PartialFilterExpression = Builders<HistoricalExistenceReportDocument>.Filter.Eq(
                    static document => document.Status,
                    HistoricalExistenceReportStatus.Pending),
            });
        return new[] { queue, ownedVisit, parkQueue, duplicate };
    }
}
