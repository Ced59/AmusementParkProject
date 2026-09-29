using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public static class LiveQualityIncidentMongoDefinitions
{
    public static IReadOnlyCollection<CreateIndexModel<LiveQualityIncidentDocument>> BuildIndexes()
    {
        return new CreateIndexModel<LiveQualityIncidentDocument>[]
        {
            new(
                Builders<LiveQualityIncidentDocument>.IndexKeys
                    .Ascending(static document => document.Status)
                    .Ascending(static document => document.Reason)
                    .Ascending(static document => document.LastReplayAttemptAtUtc)
                    .Ascending(static document => document.DetectedAtUtc),
                new CreateIndexOptions { Name = "idx_live_quality_status_reason_detected" }),
            new(
                Builders<LiveQualityIncidentDocument>.IndexKeys
                    .Ascending(static document => document.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "idx_live_quality_expiration_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };
    }

    public static FilterDefinition<LiveQualityIncidentDocument> BuildReplayCandidatesFilter(
        DateTime nowUtc)
    {
        return Builders<LiveQualityIncidentDocument>.Filter.And(
            Builders<LiveQualityIncidentDocument>.Filter.Eq(
                static document => document.Status,
                LiveQualityIncidentStatus.Pending),
            Builders<LiveQualityIncidentDocument>.Filter.In(
                static document => document.Reason,
                new[]
                {
                    LiveQualityIncidentReason.UnmappedTarget,
                    LiveQualityIncidentReason.IneligibleMapping,
                    LiveQualityIncidentReason.InvalidFreshness,
                }),
            Builders<LiveQualityIncidentDocument>.Filter.Gt(
                static document => document.ExpiresAtUtc,
                nowUtc),
            Builders<LiveQualityIncidentDocument>.Filter.Ne(
                static document => document.Observation,
                (ExternalLiveObservationDocument?)null));
    }

    public static SortDefinition<LiveQualityIncidentDocument> BuildReplayCandidatesSort()
    {
        return Builders<LiveQualityIncidentDocument>.Sort
            .Ascending(static document => document.LastReplayAttemptAtUtc)
            .Ascending(static document => document.DetectedAtUtc);
    }
}
