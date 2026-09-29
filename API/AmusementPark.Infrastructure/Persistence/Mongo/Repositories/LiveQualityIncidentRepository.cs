using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LiveQualityIncidentRepository : ILiveQualityIncidentRepository
{
    private readonly IMongoCollection<LiveQualityIncidentDocument> collection;

    public LiveQualityIncidentRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<LiveQualityIncidentDocument>(
            settings.LiveQualityIncidentsCollectionName);
    }

    public async Task SaveAsync(
        IReadOnlyCollection<LiveQualityIncident> incidents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incidents);
        if (incidents.Count == 0)
        {
            return;
        }

        List<WriteModel<LiveQualityIncidentDocument>> writes = incidents
            .Select(static incident => incident.ToDocument())
            .GroupBy(static document => document.Id, StringComparer.Ordinal)
            .Select(static group => group.First())
            .Select(static document => new UpdateOneModel<LiveQualityIncidentDocument>(
                Builders<LiveQualityIncidentDocument>.Filter.Eq(
                    static stored => stored.Id,
                    document.Id),
                BuildInsert(document))
            {
                IsUpsert = true,
            })
            .Cast<WriteModel<LiveQualityIncidentDocument>>()
            .ToList();
        await this.collection.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<LiveQualityIncident>> GetReplayCandidatesAsync(
        int maximumCount,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        List<LiveQualityIncidentDocument> documents = await this.collection
            .Find(LiveQualityIncidentMongoDefinitions.BuildReplayCandidatesFilter(nowUtc))
            .Sort(LiveQualityIncidentMongoDefinitions.BuildReplayCandidatesSort())
            .Limit(maximumCount)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<int> MarkResolvedAsync(
        IReadOnlyCollection<Guid> incidentIds,
        DateTime resolvedAtUtc,
        string resolvedByUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incidentIds);
        if (incidentIds.Count == 0)
        {
            return 0;
        }

        string[] ids = incidentIds.Select(static id => id.ToString("N")).ToArray();
        FilterDefinition<LiveQualityIncidentDocument> filter =
            Builders<LiveQualityIncidentDocument>.Filter.In(
                static document => document.IncidentId,
                ids)
            & Builders<LiveQualityIncidentDocument>.Filter.Eq(
                static document => document.Status,
                LiveQualityIncidentStatus.Pending);
        UpdateDefinition<LiveQualityIncidentDocument> update =
            Builders<LiveQualityIncidentDocument>.Update
                .Set(static document => document.Status, LiveQualityIncidentStatus.Resolved)
                .Set(static document => document.ResolvedAtUtc, resolvedAtUtc)
                .Set(static document => document.ResolvedByUserId, resolvedByUserId)
                .Set(static document => document.UpdatedAt, resolvedAtUtc);
        UpdateResult result = await this.collection.UpdateManyAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return checked((int)result.ModifiedCount);
    }

    public async Task MarkReplayAttemptedAsync(
        IReadOnlyCollection<Guid> incidentIds,
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incidentIds);
        if (incidentIds.Count == 0)
        {
            return;
        }

        string[] ids = incidentIds.Select(static id => id.ToString("N")).ToArray();
        FilterDefinition<LiveQualityIncidentDocument> filter =
            Builders<LiveQualityIncidentDocument>.Filter.In(
                static document => document.IncidentId,
                ids)
            & Builders<LiveQualityIncidentDocument>.Filter.Eq(
                static document => document.Status,
                LiveQualityIncidentStatus.Pending);
        UpdateDefinition<LiveQualityIncidentDocument> update =
            Builders<LiveQualityIncidentDocument>.Update
                .Inc(static document => document.ReplayAttemptCount, 1)
                .Set(static document => document.LastReplayAttemptAtUtc, attemptedAtUtc)
                .Set(static document => document.UpdatedAt, attemptedAtUtc);
        await this.collection.UpdateManyAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    internal static UpdateDefinition<LiveQualityIncidentDocument> BuildInsert(
        LiveQualityIncidentDocument document)
    {
        return Builders<LiveQualityIncidentDocument>.Update
            .SetOnInsert(static stored => stored.Id, document.Id)
            .SetOnInsert(static stored => stored.IncidentId, document.IncidentId)
            .SetOnInsert(static stored => stored.CreatedAt, document.CreatedAt)
            .SetOnInsert(static stored => stored.UpdatedAt, document.UpdatedAt)
            .SetOnInsert(static stored => stored.SourceId, document.SourceId)
            .SetOnInsert(static stored => stored.Observation, document.Observation)
            .SetOnInsert(static stored => stored.Reason, document.Reason)
            .SetOnInsert(static stored => stored.DiagnosticCode, document.DiagnosticCode)
            .SetOnInsert(
                static stored => stored.DiagnosticExternalTargetId,
                document.DiagnosticExternalTargetId)
            .SetOnInsert(static stored => stored.DiagnosticField, document.DiagnosticField)
            .SetOnInsert(static stored => stored.ReceivedAtUtc, document.ReceivedAtUtc)
            .SetOnInsert(static stored => stored.ReceivedAtUtcTicks, document.ReceivedAtUtcTicks)
            .SetOnInsert(static stored => stored.DetectedAtUtc, document.DetectedAtUtc)
            .SetOnInsert(static stored => stored.DetectedAtUtcTicks, document.DetectedAtUtcTicks)
            .SetOnInsert(static stored => stored.ExpiresAtUtc, document.ExpiresAtUtc)
            .SetOnInsert(static stored => stored.CorrelationId, document.CorrelationId)
            .SetOnInsert(static stored => stored.AdapterVersion, document.AdapterVersion)
            .SetOnInsert(static stored => stored.UsagePolicyVersion, document.UsagePolicyVersion)
            .SetOnInsert(
                static stored => stored.TransformationVersion,
                document.TransformationVersion)
            .SetOnInsert(static stored => stored.Confidence, document.Confidence)
            .SetOnInsert(static stored => stored.FreshnessPolicy, document.FreshnessPolicy)
            .SetOnInsert(static stored => stored.PayloadSha256, document.PayloadSha256)
            .SetOnInsert(static stored => stored.Status, document.Status)
            .SetOnInsert(static stored => stored.ReplayAttemptCount, document.ReplayAttemptCount);
    }
}
