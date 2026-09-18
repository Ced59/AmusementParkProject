using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripParkCandidateRepository : ITripParkCandidateRepository
{
    private readonly IMongoCollection<TripParkCandidateDocument> collection;
    private readonly IMongoCollection<TripPlanDocument> tripPlanCollection;

    public TripParkCandidateRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripParkCandidateDocument>(
            settings.TripParkCandidatesCollectionName);
        this.tripPlanCollection = database.GetCollection<TripPlanDocument>(
            settings.TripPlansCollectionName);
    }

    internal TripParkCandidateRepository(
        IMongoCollection<TripParkCandidateDocument> collection,
        IMongoCollection<TripPlanDocument> tripPlanCollection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.tripPlanCollection = tripPlanCollection ?? throw new ArgumentNullException(nameof(tripPlanCollection));
    }

    internal static IReadOnlyCollection<CreateIndexModel<TripParkCandidateDocument>> BuildIndexes()
    {
        FilterDefinitionBuilder<TripParkCandidateDocument> filters = Builders<TripParkCandidateDocument>.Filter;
        return new List<CreateIndexModel<TripParkCandidateDocument>>
        {
            new(
                Builders<TripParkCandidateDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.ParkId),
                new CreateIndexOptions<TripParkCandidateDocument>
                {
                    Unique = true,
                    Name = "uq_trip_candidate_plan_park",
                }),
            new(
                Builders<TripParkCandidateDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.OperationId),
                new CreateIndexOptions<TripParkCandidateDocument>
                {
                    Unique = true,
                    Name = "uq_trip_candidate_plan_operation",
                }),
            new(
                Builders<TripParkCandidateDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.DocumentState)
                    .Ascending(static document => document.SortPosition)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "ix_trip_candidate_plan_order" }),
            new(
                Builders<TripParkCandidateDocument>.IndexKeys
                    .Ascending(static document => document.ReservedExpiresAtUtc),
                new CreateIndexOptions<TripParkCandidateDocument>
                {
                    Name = "ttl_trip_candidate_reserved",
                    ExpireAfter = TimeSpan.Zero,
                    PartialFilterExpression = filters.Eq(
                        static document => document.DocumentState,
                        TripChildDocumentState.Reserved),
                }),
            new(
                Builders<TripParkCandidateDocument>.IndexKeys
                    .Ascending(static document => document.TombstoneExpiresAtUtc),
                new CreateIndexOptions<TripParkCandidateDocument>
                {
                    Name = "ttl_trip_candidate_creation_tombstone",
                    ExpireAfter = TimeSpan.Zero,
                }),
            TripActivityPendingMongoDefinitions.BuildPendingIndex<TripParkCandidateDocument>(
                "ix_trip_candidate_pending_audit"),
        };
    }

    public async Task<IReadOnlyCollection<TripParkCandidate>> ListAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        (IReadOnlyCollection<TripParkCandidate> Candidates, TripPlanDocument? Plan) state =
            await this.LoadOrderedStateAsync(tripPlanId, cancellationToken);
        return state.Candidates;
    }

    public async Task<TripParkCandidate?> GetAsync(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TripParkCandidate> candidates = await this.ListAsync(
            tripPlanId,
            cancellationToken);
        return candidates.SingleOrDefault(candidate => candidate.Id == candidateId);
    }

    public async Task<TripParkCandidateWriteResult> ResolveCreationAsync(
        TripPlanId tripPlanId,
        string operationId,
        string requestHash,
        CancellationToken cancellationToken)
    {
        string normalizedOperationId = NormalizeRequired(operationId, nameof(operationId));
        string normalizedRequestHash = NormalizeRequired(requestHash, nameof(requestHash));
        FilterDefinitionBuilder<TripParkCandidateDocument> filters =
            Builders<TripParkCandidateDocument>.Filter;
        TripParkCandidateDocument? document = await this.collection.Find(
                filters.Eq(static candidate => candidate.TripPlanId, tripPlanId.Value)
                & filters.Eq(static candidate => candidate.OperationId, normalizedOperationId))
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return new TripParkCandidateWriteResult(TripChildWriteOutcome.NotFound);
        }

        if (!string.Equals(document.RequestHash, normalizedRequestHash, StringComparison.Ordinal))
        {
            return new TripParkCandidateWriteResult(TripChildWriteOutcome.IdempotencyConflict);
        }

        return document.DocumentState switch
        {
            TripChildDocumentState.Committed =>
                await this.BuildSuccessResultAsync(document, true, cancellationToken),
            TripChildDocumentState.Deleted =>
                new TripParkCandidateWriteResult(TripChildWriteOutcome.Deleted),
            _ => new TripParkCandidateWriteResult(TripChildWriteOutcome.LeaseExpired),
        };
    }

    private async Task<(IReadOnlyCollection<TripParkCandidate> Candidates, TripPlanDocument? Plan)>
        LoadOrderedStateAsync(
            TripPlanId tripPlanId,
            CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripParkCandidateDocument> filters = Builders<TripParkCandidateDocument>.Filter;
        Task<List<TripParkCandidateDocument>> documentsTask = this.collection.Find(
                filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
                & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed))
            .SortBy(static document => document.SortPosition)
            .ThenBy(static document => document.CreatedAt)
            .ThenBy(static document => document.Id)
            .Limit(TripParkCandidate.MaximumCandidatesPerTrip)
            .ToListAsync(cancellationToken);
        FilterDefinitionBuilder<TripPlanDocument> planFilters = Builders<TripPlanDocument>.Filter;
        Task<TripPlanDocument> planTask = this.tripPlanCollection.Find(
                planFilters.Eq(static document => document.Id, tripPlanId.Value)
                & planFilters.Eq(static document => document.DeletionState, TripDeletionState.None))
            .FirstOrDefaultAsync(cancellationToken);
        await Task.WhenAll(documentsTask, planTask);
        TripPlanDocument? plan = await planTask;
        IReadOnlyList<TripParkCandidateDocument> ordered = OrderDocuments(
            await documentsTask,
            plan?.ParkCandidateOrderIds);
        TripParkCandidate[] candidates = ordered.Select(static (document, index) => document.ToDomain(
            checked((index + 1L) * TripParkCandidate.SortPositionStep))).ToArray();
        return (candidates, plan);
    }

    internal static IReadOnlyList<TripParkCandidateDocument> OrderDocuments(
        IReadOnlyCollection<TripParkCandidateDocument> documents,
        IReadOnlyCollection<string>? orderedCandidateIds)
    {
        ArgumentNullException.ThrowIfNull(documents);
        Dictionary<string, TripParkCandidateDocument> byId = documents.ToDictionary(
            static document => document.Id,
            StringComparer.Ordinal);
        List<TripParkCandidateDocument> ordered = new(documents.Count);
        if (orderedCandidateIds is not null)
        {
            foreach (string candidateId in orderedCandidateIds)
            {
                if (byId.Remove(candidateId, out TripParkCandidateDocument? document))
                {
                    ordered.Add(document);
                }
            }
        }

        ordered.AddRange(byId.Values
            .OrderBy(static document => document.SortPosition)
            .ThenBy(static document => document.CreatedAt)
            .ThenBy(static document => document.Id, StringComparer.Ordinal));
        return ordered;
    }

    public async Task<TripParkCandidateWriteResult> CreateAsync(
        TripParkCandidate candidate,
        TripChildMutationLease lease,
        string requestHash,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(lease);
        string normalizedRequestHash = NormalizeRequired(requestHash, nameof(requestHash));
        TripParkCandidateDocument shell = new()
        {
            Id = candidate.Id.Value,
            TripPlanId = candidate.TripPlanId.Value,
            ParkId = candidate.ParkId,
            DocumentState = TripChildDocumentState.Reserved,
            OperationId = lease.OperationId,
            RequestHash = normalizedRequestHash,
            ChildMutationEpoch = lease.ChildMutationEpoch,
            LeaseGeneration = lease.Generation,
            LeaseExpiresAtUtc = lease.ExpiresAtUtc,
            ReservedExpiresAtUtc = lease.ExpiresAtUtc.AddMinutes(5),
            CreatedAt = candidate.CreatedAtUtc,
            UpdatedAt = candidate.UpdatedAtUtc,
        };
        try
        {
            await this.collection.InsertOneAsync(shell, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            FilterDefinitionBuilder<TripParkCandidateDocument> duplicateFilters =
                Builders<TripParkCandidateDocument>.Filter;
            TripParkCandidateDocument? operationReplay = await this.collection.Find(
                    duplicateFilters.Eq(
                        static document => document.TripPlanId,
                        candidate.TripPlanId.Value)
                    & duplicateFilters.Eq(static document => document.OperationId, lease.OperationId))
                .FirstOrDefaultAsync(cancellationToken);
            if (operationReplay is not null
                && !string.Equals(
                    operationReplay.RequestHash,
                    normalizedRequestHash,
                    StringComparison.Ordinal))
            {
                return new TripParkCandidateWriteResult(TripChildWriteOutcome.IdempotencyConflict);
            }

            TripParkCandidateDocument? existing = operationReplay ?? await this.collection.Find(
                    duplicateFilters.Eq(
                        static document => document.TripPlanId,
                        candidate.TripPlanId.Value)
                    & duplicateFilters.Eq(static document => document.ParkId, candidate.ParkId))
                .FirstOrDefaultAsync(cancellationToken);
            if (operationReplay is null || existing is null)
            {
                return new TripParkCandidateWriteResult(TripChildWriteOutcome.Duplicate);
            }

            if (existing.DocumentState == TripChildDocumentState.Committed)
            {
                return await this.BuildSuccessResultAsync(existing, true, cancellationToken);
            }

            TripParkCandidateDocument? reclaimed = await this.collection.FindOneAndUpdateAsync(
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
                        | TripChildMutationMongoDefinitions.BuildExpiredCreationLeaseGuard<TripParkCandidateDocument>()),
                Builders<TripParkCandidateDocument>.Update
                    .Set(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
                    .Set(static document => document.LeaseGeneration, lease.Generation)
                    .Set(static document => document.LeaseExpiresAtUtc, lease.ExpiresAtUtc)
                    .Set(static document => document.ReservedExpiresAtUtc, lease.ExpiresAtUtc.AddMinutes(5)),
                new FindOneAndUpdateOptions<TripParkCandidateDocument, TripParkCandidateDocument>
                {
                    ReturnDocument = ReturnDocument.After,
                },
                cancellationToken);
            if (reclaimed is null)
            {
                return new TripParkCandidateWriteResult(TripChildWriteOutcome.LeaseExpired);
            }

            candidate = TripParkCandidate.Create(
                TripParkCandidateId.Parse(reclaimed.Id),
                candidate.TripPlanId,
                candidate.ParkId,
                candidate.CandidateDates,
                candidate.Source,
                candidate.CollectiveNote,
                candidate.FitSnapshot,
                candidate.AddedByMemberId,
                candidate.SortPosition,
                DateTime.SpecifyKind(reclaimed.CreatedAt, DateTimeKind.Utc));
        }

        TripParkCandidateDocument materialized = candidate.ToDocument();
        FilterDefinitionBuilder<TripParkCandidateDocument> filters = Builders<TripParkCandidateDocument>.Filter;
        FilterDefinition<TripParkCandidateDocument> filter = filters.Eq(
                static document => document.Id,
                candidate.Id.Value)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Reserved)
            & filters.Eq(static document => document.OperationId, lease.OperationId)
            & filters.Eq(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
            & filters.Eq(static document => document.LeaseGeneration, lease.Generation)
            & TripChildMutationMongoDefinitions.BuildCreationLeaseGuard<TripParkCandidateDocument>();
        UpdateDefinitionBuilder<TripParkCandidateDocument> updates = Builders<TripParkCandidateDocument>.Update;
        UpdateDefinition<TripParkCandidateDocument> update = TripActivityPendingMongoDefinitions.Append(updates
            .Set(static document => document.CandidateDates, materialized.CandidateDates)
            .Set(static document => document.Source, materialized.Source)
            .Set(static document => document.CandidateState, materialized.CandidateState)
            .Set(static document => document.CollectiveNote, materialized.CollectiveNote)
            .Set(static document => document.FitSnapshot, materialized.FitSnapshot)
            .Set(static document => document.AddedByMemberId, materialized.AddedByMemberId)
            .Set(static document => document.SortPosition, materialized.SortPosition)
            .Set(static document => document.Version, materialized.Version)
            .Set(static document => document.CreatedAt, materialized.CreatedAt)
            .Set(static document => document.UpdatedAt, materialized.UpdatedAt)
            .Set(static document => document.DocumentState, TripChildDocumentState.Committed)
            .Unset(static document => document.ReservedExpiresAtUtc), pendingActivity);
        TripParkCandidateDocument? committed = await this.collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<TripParkCandidateDocument, TripParkCandidateDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (committed is not null)
        {
            return await this.BuildSuccessResultAsync(committed, false, cancellationToken);
        }

        TripParkCandidateDocument? replayed = await this.collection.Find(
                filters.Eq(static document => document.Id, candidate.Id.Value)
                & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed)
                & filters.Eq(static document => document.OperationId, lease.OperationId)
                & filters.Eq(static document => document.RequestHash, normalizedRequestHash))
            .FirstOrDefaultAsync(cancellationToken);
        return replayed is null
            ? new TripParkCandidateWriteResult(TripChildWriteOutcome.LeaseExpired)
            : await this.BuildSuccessResultAsync(replayed, true, cancellationToken);
    }

    public async Task<TripParkCandidateWriteResult> ReplaceAsync(
        TripParkCandidate candidate,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(lease);
        TripParkCandidateDocument? reserved = await this.ReserveMutationAsync(
            candidate.TripPlanId,
            candidate.Id,
            expectedVersion,
            lease,
            cancellationToken);
        if (reserved is null)
        {
            return await this.ResolveFailedWriteAsync(
                candidate.TripPlanId,
                candidate.Id,
                cancellationToken);
        }

        TripParkCandidateDocument replacement = candidate.ToDocument();
        FilterDefinition<TripParkCandidateDocument> commitFilter = BuildPendingFilter(
            candidate.TripPlanId,
            candidate.Id,
            expectedVersion,
            lease);
        UpdateDefinitionBuilder<TripParkCandidateDocument> updates = Builders<TripParkCandidateDocument>.Update;
        UpdateDefinition<TripParkCandidateDocument> update = TripActivityPendingMongoDefinitions.Append(updates
            .Set(static document => document.CandidateDates, replacement.CandidateDates)
            .Set(static document => document.CandidateState, replacement.CandidateState)
            .Set(static document => document.CollectiveNote, replacement.CollectiveNote)
            .Set(static document => document.FitSnapshot, replacement.FitSnapshot)
            .Set(static document => document.SortPosition, replacement.SortPosition)
            .Set(static document => document.Version, replacement.Version)
            .Set(static document => document.UpdatedAt, replacement.UpdatedAt)
            .Unset(static document => document.PendingMutation), pendingActivity);
        TripParkCandidateDocument? committed = await this.collection.FindOneAndUpdateAsync(
            commitFilter,
            update,
            new FindOneAndUpdateOptions<TripParkCandidateDocument, TripParkCandidateDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        return committed is null
            ? new TripParkCandidateWriteResult(TripChildWriteOutcome.LeaseExpired)
            : await this.BuildSuccessResultAsync(committed, false, cancellationToken);
    }

    public async Task<TripChildWriteOutcome> ApplyOrderAsync(
        TripPlanId tripPlanId,
        TripParkCandidateOrderPlan orderPlan,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orderPlan);
        ArgumentNullException.ThrowIfNull(lease);
        if (orderPlan.Changes.Count == 0)
        {
            return TripChildWriteOutcome.Success;
        }

        (IReadOnlyCollection<TripParkCandidate> existing, TripPlanDocument? currentPlan) =
            await this.LoadOrderedStateAsync(tripPlanId, cancellationToken);
        if (currentPlan is null)
        {
            return TripChildWriteOutcome.NotFound;
        }

        Dictionary<TripParkCandidateId, TripParkCandidate> byId = existing.ToDictionary(static candidate => candidate.Id);
        bool guardsMatch = orderPlan.Guards.All(guard => byId.TryGetValue(guard.CandidateId, out TripParkCandidate? candidate)
            && candidate.Version == guard.Version
            && candidate.SortPosition == guard.SortPosition);
        if (!guardsMatch)
        {
            return TripChildWriteOutcome.Conflict;
        }

        Dictionary<TripParkCandidateId, long> positions = existing.ToDictionary(
            static candidate => candidate.Id,
            static candidate => candidate.SortPosition);
        foreach (TripParkCandidateOrderPosition change in orderPlan.Changes)
        {
            if (!byId.TryGetValue(change.CandidateId, out TripParkCandidate? candidate)
                || candidate.Version != change.ExpectedVersion)
            {
                return TripChildWriteOutcome.Conflict;
            }

            positions[change.CandidateId] = change.SortPosition;
        }

        List<string> orderedIds = existing
            .OrderBy(candidate => positions[candidate.Id])
            .ThenBy(static candidate => candidate.CreatedAtUtc)
            .ThenBy(static candidate => candidate.Id.Value, StringComparer.Ordinal)
            .Select(static candidate => candidate.Id.Value)
            .ToList();
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> filter = filters.Eq(
                static document => document.Id,
                tripPlanId.Value)
            & filters.Eq(static document => document.DeletionState, TripDeletionState.None)
            & filters.Eq(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
            & filters.Eq(
                static document => document.ParkCandidateOrderVersion,
                currentPlan.ParkCandidateOrderVersion)
            & TripPlanMongoDefinitions.BuildActiveChildLeaseIdentityFilter(lease);
        UpdateDefinition<TripPlanDocument> update = TripActivityPendingMongoDefinitions.Append(
            Builders<TripPlanDocument>.Update
                .Set(static document => document.ParkCandidateOrderIds, orderedIds)
                .Inc(static document => document.ParkCandidateOrderVersion, 1),
            pendingActivity);
        UpdateResult result = await this.tripPlanCollection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1
            ? TripChildWriteOutcome.Success
            : TripChildWriteOutcome.Conflict;
    }

    public async Task<TripParkCandidateWriteResult> DeleteAsync(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId,
        long expectedVersion,
        TripChildMutationLease lease,
        DateTime deletedAtUtc,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        if (deletedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The deletion timestamp must be UTC.", nameof(deletedAtUtc));
        }

        TripParkCandidateDocument? reserved = await this.ReserveMutationAsync(
            tripPlanId,
            candidateId,
            expectedVersion,
            lease,
            cancellationToken);
        if (reserved is null)
        {
            return await this.ResolveFailedWriteAsync(tripPlanId, candidateId, cancellationToken);
        }

        UpdateDefinitionBuilder<TripParkCandidateDocument> updates =
            Builders<TripParkCandidateDocument>.Update;
        UpdateDefinition<TripParkCandidateDocument> tombstoneExpiry =
            BuildDeletionTombstoneExpiryUpdate(deletedAtUtc, pendingActivity is not null);
        UpdateDefinition<TripParkCandidateDocument> tombstone = TripActivityPendingMongoDefinitions.Append(updates.Combine(
            updates.Set(static document => document.ParkId, $"deleted:{candidateId.Value}"),
            updates.Set(static document => document.CandidateDates, new List<string>()),
            updates.Set(static document => document.Source, TripParkCandidateSource.Manual),
            updates.Set(static document => document.CandidateState, TripParkCandidateState.Rejected),
            updates.Unset(static document => document.CollectiveNote),
            updates.Unset(static document => document.FitSnapshot),
            updates.Set(static document => document.AddedByMemberId, string.Empty),
            updates.Set(static document => document.SortPosition, 0),
            updates.Set(static document => document.Version, checked(expectedVersion + 1)),
            updates.Set(static document => document.DocumentState, TripChildDocumentState.Deleted),
            updates.Unset(static document => document.ReservedExpiresAtUtc),
            tombstoneExpiry,
            updates.Unset(static document => document.PendingMutation),
            updates.Set(static document => document.CreatedAt, deletedAtUtc),
            updates.Set(static document => document.UpdatedAt, deletedAtUtc)), pendingActivity);
        UpdateResult result = await this.collection.UpdateOneAsync(
            BuildPendingFilter(tripPlanId, candidateId, expectedVersion, lease),
            tombstone,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1
            ? new TripParkCandidateWriteResult(TripChildWriteOutcome.Success)
            : new TripParkCandidateWriteResult(TripChildWriteOutcome.LeaseExpired);
    }

    internal static UpdateDefinition<TripParkCandidateDocument> BuildDeletionTombstoneExpiryUpdate(
        DateTime deletedAtUtc,
        bool hasPendingAudit)
    {
        if (deletedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The deletion timestamp must be UTC.", nameof(deletedAtUtc));
        }

        UpdateDefinitionBuilder<TripParkCandidateDocument> updates =
            Builders<TripParkCandidateDocument>.Update;
        return hasPendingAudit
            ? updates.Unset(static document => document.TombstoneExpiresAtUtc)
            : updates.Set(
                static document => document.TombstoneExpiresAtUtc,
                deletedAtUtc.Add(TripParkCandidate.CreationReplayRetention));
    }

    private async Task<TripParkCandidateDocument?> ReserveMutationAsync(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripParkCandidateDocument> filters = Builders<TripParkCandidateDocument>.Filter;
        FilterDefinition<TripParkCandidateDocument> identity = BuildCommittedIdentityFilter(tripPlanId, candidateId)
            & filters.Eq(static document => document.Version, expectedVersion);
        FilterDefinition<TripParkCandidateDocument> freshFilter = identity
            & TripChildMutationMongoDefinitions.BuildPendingAvailableGuard<TripParkCandidateDocument>();
        TripParkCandidateDocument? reserved = await this.collection.FindOneAndUpdateAsync(
            freshFilter,
            Builders<TripParkCandidateDocument>.Update.Set(
                static document => document.PendingMutation,
                TripChildMutationMongoDefinitions.ToPendingDocument(lease)),
            new FindOneAndUpdateOptions<TripParkCandidateDocument, TripParkCandidateDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (reserved is not null)
        {
            return reserved;
        }

        TripParkCandidateDocument? replay = await this.collection.Find(identity).FirstOrDefaultAsync(cancellationToken);
        return TripChildMutationMongoDefinitions.HasSameIdentity(replay?.PendingMutation, lease)
            ? replay
            : null;
    }

    private async Task<TripParkCandidateWriteResult> ResolveFailedWriteAsync(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId,
        CancellationToken cancellationToken)
    {
        TripParkCandidateDocument? existing = await this.collection.Find(BuildCommittedIdentityFilter(
                tripPlanId,
                candidateId))
            .FirstOrDefaultAsync(cancellationToken);
        return existing is null
            ? new TripParkCandidateWriteResult(TripChildWriteOutcome.NotFound)
            : new TripParkCandidateWriteResult(
                TripChildWriteOutcome.Conflict,
                existing.ToDomain(),
                existing.Version);
    }

    private async Task<TripParkCandidateWriteResult> BuildSuccessResultAsync(
        TripParkCandidateDocument document,
        bool wasReplayed,
        CancellationToken cancellationToken)
    {
        TripParkCandidate? candidate = await this.GetAsync(
            TripPlanId.Parse(document.TripPlanId),
            TripParkCandidateId.Parse(document.Id),
            cancellationToken);
        return candidate is null
            ? new TripParkCandidateWriteResult(TripChildWriteOutcome.LeaseExpired)
            : new TripParkCandidateWriteResult(
                TripChildWriteOutcome.Success,
                candidate,
                candidate.Version,
                wasReplayed);
    }

    private static FilterDefinition<TripParkCandidateDocument> BuildCommittedIdentityFilter(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId)
    {
        FilterDefinitionBuilder<TripParkCandidateDocument> filters = Builders<TripParkCandidateDocument>.Filter;
        return filters.Eq(static document => document.Id, candidateId.Value)
            & filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed);
    }

    private static FilterDefinition<TripParkCandidateDocument> BuildPendingFilter(
        TripPlanId tripPlanId,
        TripParkCandidateId candidateId,
        long expectedVersion,
        TripChildMutationLease lease)
    {
        FilterDefinitionBuilder<TripParkCandidateDocument> filters = Builders<TripParkCandidateDocument>.Filter;
        return BuildCommittedIdentityFilter(tripPlanId, candidateId)
            & filters.Eq(static document => document.Version, expectedVersion)
            & filters.Eq("pendingMutation.operationId", lease.OperationId)
            & filters.Eq("pendingMutation.childMutationEpoch", lease.ChildMutationEpoch)
            & filters.Eq("pendingMutation.leaseGeneration", lease.Generation)
            & TripChildMutationMongoDefinitions.BuildPendingLeaseGuard<TripParkCandidateDocument>();
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
