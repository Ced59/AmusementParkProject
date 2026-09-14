using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkFitSourceReportMongoDefinitions
{
    public const string StatusQueueIndexName = "idx_park_fit_reports_status_submitted";
    public const string ParkStatusIndexName = "idx_park_fit_reports_park_status";

    public static FilterDefinition<ParkFitSourceReportDocument> BuildIdFilter(string id)
    {
        return Builders<ParkFitSourceReportDocument>.Filter.Eq(
            static document => document.Id,
            id);
    }

    public static FilterDefinition<ParkFitSourceReportDocument> BuildRevisionFilter(
        string id,
        long revision)
    {
        return BuildIdFilter(id)
            & Builders<ParkFitSourceReportDocument>.Filter.Eq(
                static document => document.Revision,
                revision);
    }

    public static FilterDefinition<ParkFitSourceReportDocument> BuildSearchFilter(
        ParkFitSourceReportSearchCriteria criteria)
    {
        FilterDefinitionBuilder<ParkFitSourceReportDocument> builder =
            Builders<ParkFitSourceReportDocument>.Filter;
        List<FilterDefinition<ParkFitSourceReportDocument>> filters = new();
        if (criteria.Status.HasValue)
        {
            filters.Add(builder.Eq(static document => document.Status, criteria.Status.Value));
        }

        if (criteria.Reason.HasValue)
        {
            filters.Add(builder.Eq(static document => document.Reason, criteria.Reason.Value));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ParkId))
        {
            filters.Add(builder.Eq(static document => document.ParkId, criteria.ParkId.Trim()));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    public static IReadOnlyCollection<CreateIndexModel<ParkFitSourceReportDocument>> BuildIndexes()
    {
        CreateIndexModel<ParkFitSourceReportDocument> queue = new(
            Builders<ParkFitSourceReportDocument>.IndexKeys
                .Ascending(static document => document.Status)
                .Descending(static document => document.SubmittedAtUtc)
                .Descending(static document => document.Id),
            new CreateIndexOptions { Name = StatusQueueIndexName });
        CreateIndexModel<ParkFitSourceReportDocument> parkQueue = new(
            Builders<ParkFitSourceReportDocument>.IndexKeys
                .Ascending(static document => document.ParkId)
                .Ascending(static document => document.Status)
                .Descending(static document => document.SubmittedAtUtc),
            new CreateIndexOptions { Name = ParkStatusIndexName });
        return new[] { queue, parkQueue };
    }
}
