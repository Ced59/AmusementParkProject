using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripAuditRepository : ITripAuditWriter, ITripAuditReader
{
    private readonly IMongoCollection<TripPlanDocument> plans;
    private readonly IMongoCollection<TripActivityEventDocument> activities;

    public TripAuditRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.plans = database.GetCollection<TripPlanDocument>(settings.TripPlansCollectionName);
        this.activities = database.GetCollection<TripActivityEventDocument>(
            settings.TripAuditEventsCollectionName);
    }

    internal TripAuditRepository(
        IMongoCollection<TripPlanDocument> plans,
        IMongoCollection<TripActivityEventDocument> activities)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.activities = activities ?? throw new ArgumentNullException(nameof(activities));
    }

    public async Task<TripActivityEvent> AppendAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        TripActivityEventDocument? existing = await this.FindByOperationAsync(
            activity.TripPlanId,
            activity.OperationKey,
            cancellationToken);
        if (existing is not null)
        {
            return ToDomain(existing);
        }

        TripPlanDocument? updatedPlan = await this.plans.FindOneAndUpdateAsync(
            Builders<TripPlanDocument>.Filter.Eq(
                static document => document.Id,
                activity.TripPlanId.Value)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.DeletionState,
                    TripDeletionState.None),
            Builders<TripPlanDocument>.Update.Inc(
                static document => document.AuditSequence,
                1),
            new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (updatedPlan is null)
        {
            throw new InvalidOperationException("The trip no longer accepts audit events.");
        }

        TripActivityEventDocument document = new TripActivityEventDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            TripPlanId = activity.TripPlanId.Value,
            ActorMemberId = activity.ActorMemberId?.Value,
            ActorRole = activity.ActorRole,
            Kind = activity.Kind,
            OperationKey = activity.OperationKey,
            Sequence = updatedPlan.AuditSequence,
            AffectedCount = activity.AffectedCount,
            CreatedAt = activity.OccurredAtUtc,
            UpdatedAt = activity.OccurredAtUtc,
        };
        try
        {
            await this.activities.InsertOneAsync(document, cancellationToken: cancellationToken);
            await this.RemoveIfTripDeletionStartedAsync(document, cancellationToken);
            return ToDomain(document);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            TripActivityEventDocument? replay = await this.FindByOperationAsync(
                activity.TripPlanId,
                activity.OperationKey,
                cancellationToken);
            if (replay is null)
            {
                throw;
            }

            return ToDomain(replay);
        }
    }

    private async Task RemoveIfTripDeletionStartedAsync(
        TripActivityEventDocument document,
        CancellationToken cancellationToken)
    {
        long activeTripCount = await this.plans.CountDocumentsAsync(
            Builders<TripPlanDocument>.Filter.Eq(
                static plan => plan.Id,
                document.TripPlanId)
            & Builders<TripPlanDocument>.Filter.Eq(
                static plan => plan.DeletionState,
                TripDeletionState.None),
            new CountOptions { Limit = 1 },
            cancellationToken);
        if (activeTripCount == 0)
        {
            _ = await this.activities.DeleteOneAsync(
                Builders<TripActivityEventDocument>.Filter.Eq(
                    static activity => activity.Id,
                    document.Id),
                cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<TripActivityEvent>> ListAsync(
        TripPlanId tripPlanId,
        long? beforeSequence,
        int limit,
        CancellationToken cancellationToken)
    {
        if (beforeSequence is < 1 || limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        FilterDefinition<TripActivityEventDocument> filter =
            Builders<TripActivityEventDocument>.Filter.Eq(
                static document => document.TripPlanId,
                tripPlanId.Value);
        if (beforeSequence.HasValue)
        {
            filter &= Builders<TripActivityEventDocument>.Filter.Lt(
                static document => document.Sequence,
                beforeSequence.Value);
        }

        List<TripActivityEventDocument> documents = await this.activities.Find(filter)
            .SortByDescending(static document => document.Sequence)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToArray();
    }

    public static IReadOnlyCollection<CreateIndexModel<TripActivityEventDocument>> BuildIndexes()
    {
        IndexKeysDefinitionBuilder<TripActivityEventDocument> keys =
            Builders<TripActivityEventDocument>.IndexKeys;
        return new List<CreateIndexModel<TripActivityEventDocument>>
        {
            new CreateIndexModel<TripActivityEventDocument>(
                keys.Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.OperationKey),
                new CreateIndexOptions
                {
                    Name = "uq_trip_audit_operation",
                    Unique = true,
                }),
            new CreateIndexModel<TripActivityEventDocument>(
                keys.Ascending(static document => document.TripPlanId)
                    .Descending(static document => document.Sequence),
                new CreateIndexOptions
                {
                    Name = "uq_trip_audit_sequence",
                    Unique = true,
                }),
            new CreateIndexModel<TripActivityEventDocument>(
                keys.Ascending(static document => document.TripPlanId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "ix_trip_audit_date" }),
        };
    }

    private async Task<TripActivityEventDocument?> FindByOperationAsync(
        TripPlanId tripPlanId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        TripActivityEventDocument? document = await this.activities.Find(
                Builders<TripActivityEventDocument>.Filter.Eq(
                    static document => document.TripPlanId,
                    tripPlanId.Value)
                & Builders<TripActivityEventDocument>.Filter.Eq(
                    static document => document.OperationKey,
                    operationKey))
            .FirstOrDefaultAsync(cancellationToken);
        return document;
    }

    private static TripActivityEvent ToDomain(TripActivityEventDocument document)
    {
        return new TripActivityEvent(
            document.Id,
            TripPlanId.Parse(document.TripPlanId),
            string.IsNullOrWhiteSpace(document.ActorMemberId)
                ? null
                : TripMemberId.Parse(document.ActorMemberId),
            document.ActorRole,
            document.Kind,
            document.OperationKey,
            document.Sequence,
            document.AffectedCount,
            DateTime.SpecifyKind(document.CreatedAt, DateTimeKind.Utc));
    }
}
