using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripDayPlanRepository : ITripDayPlanRepository
{
    private readonly IMongoCollection<TripDayPlanDocument> collection;

    public TripDayPlanRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripDayPlanDocument>(settings.TripDayPlansCollectionName);
    }

    internal TripDayPlanRepository(IMongoCollection<TripDayPlanDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    internal static IReadOnlyCollection<CreateIndexModel<TripDayPlanDocument>> BuildIndexes()
    {
        FilterDefinitionBuilder<TripDayPlanDocument> filters = Builders<TripDayPlanDocument>.Filter;
        return new List<CreateIndexModel<TripDayPlanDocument>>
        {
            new(
                Builders<TripDayPlanDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.LocalDate),
                new CreateIndexOptions<TripDayPlanDocument>
                {
                    Unique = true,
                    Name = "uq_trip_day_plan_date",
                }),
            new(
                Builders<TripDayPlanDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.DocumentState)
                    .Ascending(static document => document.LocalDate),
                new CreateIndexOptions { Name = "ix_trip_day_plan_date" }),
            new(
                Builders<TripDayPlanDocument>.IndexKeys
                    .Ascending(static document => document.ReservedExpiresAtUtc),
                new CreateIndexOptions<TripDayPlanDocument>
                {
                    Name = "ttl_trip_day_plan_reserved",
                    ExpireAfter = TimeSpan.Zero,
                    PartialFilterExpression = filters.Eq(
                        static document => document.DocumentState,
                        TripChildDocumentState.Reserved),
                }),
            TripActivityPendingMongoDefinitions.BuildPendingIndex<TripDayPlanDocument>(
                "ix_trip_day_pending_audit"),
        };
    }

    public async Task<IReadOnlyCollection<TripDayPlan>> ListAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripDayPlanDocument> filters = Builders<TripDayPlanDocument>.Filter;
        List<TripDayPlanDocument> documents = await this.collection.Find(
                filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
                & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed))
            .SortBy(static document => document.LocalDate)
            .ThenBy(static document => document.Id)
            .Limit(TripDayPlan.MaximumDaysPerTrip)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<TripDayPlanWriteResult> PutAsync(
        TripDayPlan dayPlan,
        long? expectedVersion,
        TripChildMutationLease lease,
        string requestHash,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dayPlan);
        ArgumentNullException.ThrowIfNull(lease);
        return expectedVersion.HasValue
            ? await this.ReplaceAsync(dayPlan, expectedVersion.Value, lease, pendingActivity, cancellationToken)
            : await this.CreateAsync(dayPlan, lease, requestHash, pendingActivity, cancellationToken);
    }

    public async Task<TripDayPlanWriteResult> DeleteAsync(
        TripPlanId tripPlanId,
        DateOnly localDate,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);
        FilterDefinitionBuilder<TripDayPlanDocument> filters = Builders<TripDayPlanDocument>.Filter;
        string date = localDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        FilterDefinition<TripDayPlanDocument> identity = filters.Eq(
                static document => document.TripPlanId,
                tripPlanId.Value)
            & filters.Eq(static document => document.LocalDate, date)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed)
            & filters.Eq(static document => document.Version, expectedVersion);
        TripDayPlanDocument? reserved = await this.collection.FindOneAndUpdateAsync(
            identity & TripChildMutationMongoDefinitions.BuildPendingAvailableGuard<TripDayPlanDocument>(),
            Builders<TripDayPlanDocument>.Update.Set(
                static document => document.PendingMutation,
                TripChildMutationMongoDefinitions.ToPendingDocument(lease)),
            new FindOneAndUpdateOptions<TripDayPlanDocument, TripDayPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (reserved is null)
        {
            TripDayPlanDocument? existing = await this.collection.Find(
                    filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
                    & filters.Eq(static document => document.LocalDate, date)
                    & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed))
                .FirstOrDefaultAsync(cancellationToken);
            return existing is null
                ? new TripDayPlanWriteResult(TripChildWriteOutcome.NotFound)
                : new TripDayPlanWriteResult(
                    TripChildWriteOutcome.Conflict,
                    existing.ToDomain(),
                    existing.Version);
        }

        UpdateDefinition<TripDayPlanDocument> deleteUpdate = TripActivityPendingMongoDefinitions.Append(
            Builders<TripDayPlanDocument>.Update
                .Set(static document => document.DocumentState, TripChildDocumentState.Deleted)
                .Set(static document => document.UpdatedAt, pendingActivity?.OccurredAtUtc ?? DateTime.UtcNow)
                .Unset(static document => document.PendingMutation),
            pendingActivity);
        UpdateResult deleted = await this.collection.UpdateOneAsync(
            identity
                & filters.Eq("pendingMutation.operationId", lease.OperationId)
                & filters.Eq("pendingMutation.childMutationEpoch", lease.ChildMutationEpoch)
                & filters.Eq("pendingMutation.leaseGeneration", lease.Generation)
                & TripChildMutationMongoDefinitions.BuildPendingLeaseGuard<TripDayPlanDocument>(),
            deleteUpdate,
            cancellationToken: cancellationToken);
        return deleted.ModifiedCount == 1
            ? new TripDayPlanWriteResult(TripChildWriteOutcome.Success)
            : new TripDayPlanWriteResult(TripChildWriteOutcome.LeaseExpired);
    }

    private async Task<TripDayPlanWriteResult> CreateAsync(
        TripDayPlan dayPlan,
        TripChildMutationLease lease,
        string requestHash,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        string normalizedRequestHash = NormalizeRequired(requestHash, nameof(requestHash));
        TripDayPlanDocument shell = new()
        {
            Id = dayPlan.Id.Value,
            TripPlanId = dayPlan.TripPlanId.Value,
            LocalDate = dayPlan.LocalDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            DocumentState = TripChildDocumentState.Reserved,
            OperationId = lease.OperationId,
            RequestHash = normalizedRequestHash,
            ChildMutationEpoch = lease.ChildMutationEpoch,
            LeaseGeneration = lease.Generation,
            LeaseExpiresAtUtc = lease.ExpiresAtUtc,
            ReservedExpiresAtUtc = lease.ExpiresAtUtc.AddMinutes(5),
            CreatedAt = dayPlan.CreatedAtUtc,
            UpdatedAt = dayPlan.UpdatedAtUtc,
        };
        try
        {
            await this.collection.InsertOneAsync(shell, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            FilterDefinitionBuilder<TripDayPlanDocument> duplicateFilters =
                Builders<TripDayPlanDocument>.Filter;
            TripDayPlanDocument? existing = await this.collection.Find(
                    duplicateFilters.Eq(static document => document.TripPlanId, dayPlan.TripPlanId.Value)
                    & duplicateFilters.Eq(
                        static document => document.LocalDate,
                        dayPlan.LocalDate.ToString(
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture)))
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null
                || !string.Equals(existing.OperationId, lease.OperationId, StringComparison.Ordinal)
                || !string.Equals(existing.RequestHash, normalizedRequestHash, StringComparison.Ordinal))
            {
                return new TripDayPlanWriteResult(TripChildWriteOutcome.Duplicate);
            }

            if (existing.DocumentState == TripChildDocumentState.Committed)
            {
                return new TripDayPlanWriteResult(
                    TripChildWriteOutcome.Success,
                    existing.ToDomain(),
                    existing.Version);
            }

            TripDayPlanDocument? reclaimed = await this.collection.FindOneAndUpdateAsync(
                duplicateFilters.Eq(static document => document.Id, existing.Id)
                    & duplicateFilters.Eq(
                        static document => document.DocumentState,
                        TripChildDocumentState.Reserved)
                    & duplicateFilters.Eq(static document => document.OperationId, lease.OperationId)
                    & duplicateFilters.Eq(static document => document.RequestHash, normalizedRequestHash)
                    & (duplicateFilters.Eq(
                            static document => document.ChildMutationEpoch,
                            lease.ChildMutationEpoch)
                        & duplicateFilters.Eq(static document => document.LeaseGeneration, lease.Generation)
                        | TripChildMutationMongoDefinitions.BuildExpiredCreationLeaseGuard<TripDayPlanDocument>()),
                Builders<TripDayPlanDocument>.Update
                    .Set(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
                    .Set(static document => document.LeaseGeneration, lease.Generation)
                    .Set(static document => document.LeaseExpiresAtUtc, lease.ExpiresAtUtc)
                    .Set(static document => document.ReservedExpiresAtUtc, lease.ExpiresAtUtc.AddMinutes(5)),
                new FindOneAndUpdateOptions<TripDayPlanDocument, TripDayPlanDocument>
                {
                    ReturnDocument = ReturnDocument.After,
                },
                cancellationToken);
            if (reclaimed is null)
            {
                return new TripDayPlanWriteResult(TripChildWriteOutcome.LeaseExpired);
            }

            dayPlan = TripDayPlan.Create(
                TripDayPlanId.Parse(reclaimed.Id),
                dayPlan.TripPlanId,
                dayPlan.LocalDate,
                dayPlan.ParkCandidateId,
                dayPlan.ParkId,
                dayPlan.DesiredArrivalTime,
                dayPlan.GroupNote,
                dayPlan.Blocks,
                DateTime.SpecifyKind(reclaimed.CreatedAt, DateTimeKind.Utc));
        }

        TripDayPlanDocument materialized = dayPlan.ToDocument();
        FilterDefinitionBuilder<TripDayPlanDocument> filters = Builders<TripDayPlanDocument>.Filter;
        FilterDefinition<TripDayPlanDocument> filter = filters.Eq(static document => document.Id, dayPlan.Id.Value)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Reserved)
            & filters.Eq(static document => document.OperationId, lease.OperationId)
            & filters.Eq(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
            & filters.Eq(static document => document.LeaseGeneration, lease.Generation)
            & TripChildMutationMongoDefinitions.BuildCreationLeaseGuard<TripDayPlanDocument>();
        UpdateDefinitionBuilder<TripDayPlanDocument> updates = Builders<TripDayPlanDocument>.Update;
        UpdateDefinition<TripDayPlanDocument> update = TripActivityPendingMongoDefinitions.Append(updates
            .Set(static document => document.ParkCandidateId, materialized.ParkCandidateId)
            .Set(static document => document.ParkId, materialized.ParkId)
            .Set(static document => document.DesiredArrivalTime, materialized.DesiredArrivalTime)
            .Set(static document => document.GroupNote, materialized.GroupNote)
            .Set(static document => document.Blocks, materialized.Blocks)
            .Set(static document => document.Version, materialized.Version)
            .Set(static document => document.CreatedAt, materialized.CreatedAt)
            .Set(static document => document.UpdatedAt, materialized.UpdatedAt)
            .Set(static document => document.DocumentState, TripChildDocumentState.Committed)
            .Unset(static document => document.ReservedExpiresAtUtc), pendingActivity);
        TripDayPlanDocument? committed = await this.collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<TripDayPlanDocument, TripDayPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (committed is not null)
        {
            return new TripDayPlanWriteResult(
                TripChildWriteOutcome.Success,
                committed.ToDomain(),
                committed.Version);
        }

        TripDayPlanDocument? replayed = await this.collection.Find(
                filters.Eq(static document => document.Id, dayPlan.Id.Value)
                & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed)
                & filters.Eq(static document => document.OperationId, lease.OperationId)
                & filters.Eq(static document => document.RequestHash, normalizedRequestHash))
            .FirstOrDefaultAsync(cancellationToken);
        return replayed is null
            ? new TripDayPlanWriteResult(TripChildWriteOutcome.LeaseExpired)
            : new TripDayPlanWriteResult(
                TripChildWriteOutcome.Success,
                replayed.ToDomain(),
                replayed.Version);
    }

    private async Task<TripDayPlanWriteResult> ReplaceAsync(
        TripDayPlan dayPlan,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripDayPlanDocument> filters = Builders<TripDayPlanDocument>.Filter;
        FilterDefinition<TripDayPlanDocument> identity = filters.Eq(
                static document => document.TripPlanId,
                dayPlan.TripPlanId.Value)
            & filters.Eq(
                static document => document.LocalDate,
                dayPlan.LocalDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed)
            & filters.Eq(static document => document.Version, expectedVersion);
        TripDayPlanDocument? reserved = await this.collection.FindOneAndUpdateAsync(
            identity & TripChildMutationMongoDefinitions.BuildPendingAvailableGuard<TripDayPlanDocument>(),
            Builders<TripDayPlanDocument>.Update.Set(
                static document => document.PendingMutation,
                TripChildMutationMongoDefinitions.ToPendingDocument(lease)),
            new FindOneAndUpdateOptions<TripDayPlanDocument, TripDayPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (reserved is null)
        {
            TripDayPlanDocument? existing = await this.collection.Find(
                    filters.Eq(static document => document.TripPlanId, dayPlan.TripPlanId.Value)
                    & filters.Eq(
                        static document => document.LocalDate,
                        dayPlan.LocalDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
                    & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed))
                .FirstOrDefaultAsync(cancellationToken);
            return existing is null
                ? new TripDayPlanWriteResult(TripChildWriteOutcome.NotFound)
                : new TripDayPlanWriteResult(
                    TripChildWriteOutcome.Conflict,
                    existing.ToDomain(),
                    existing.Version);
        }

        TripDayPlanDocument replacement = dayPlan.ToDocument();
        FilterDefinition<TripDayPlanDocument> commitFilter = identity
            & filters.Eq("pendingMutation.operationId", lease.OperationId)
            & filters.Eq("pendingMutation.childMutationEpoch", lease.ChildMutationEpoch)
            & filters.Eq("pendingMutation.leaseGeneration", lease.Generation)
            & TripChildMutationMongoDefinitions.BuildPendingLeaseGuard<TripDayPlanDocument>();
        UpdateDefinitionBuilder<TripDayPlanDocument> updates = Builders<TripDayPlanDocument>.Update;
        UpdateDefinition<TripDayPlanDocument> update = TripActivityPendingMongoDefinitions.Append(updates
            .Set(static document => document.ParkCandidateId, replacement.ParkCandidateId)
            .Set(static document => document.ParkId, replacement.ParkId)
            .Set(static document => document.DesiredArrivalTime, replacement.DesiredArrivalTime)
            .Set(static document => document.GroupNote, replacement.GroupNote)
            .Set(static document => document.Blocks, replacement.Blocks)
            .Set(static document => document.Version, replacement.Version)
            .Set(static document => document.UpdatedAt, replacement.UpdatedAt)
            .Unset(static document => document.PendingMutation), pendingActivity);
        TripDayPlanDocument? committed = await this.collection.FindOneAndUpdateAsync(
            commitFilter,
            update,
            new FindOneAndUpdateOptions<TripDayPlanDocument, TripDayPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        return committed is null
            ? new TripDayPlanWriteResult(TripChildWriteOutcome.LeaseExpired)
            : new TripDayPlanWriteResult(
                TripChildWriteOutcome.Success,
                committed.ToDomain(),
                committed.Version);
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return normalized;
    }
}
