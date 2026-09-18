using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripPlanRepository : ITripPlanRepository
{
    private readonly IMongoCollection<TripPlanDocument> collection;
    private readonly IMongoCollection<TripParkCandidateDocument>? candidateCollection;
    private readonly IMongoCollection<TripDayPlanDocument>? dayPlanCollection;
    private readonly IMongoCollection<TripInvitationDocument>? invitationCollection;
    private readonly TripPlanCreationFingerprint creationFingerprint;

    public TripPlanRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        TripPlanCreationFingerprint creationFingerprint)
        : this(
            GetCollection(database, settings),
            creationFingerprint,
            GetCandidateCollection(database, settings),
            GetDayPlanCollection(database, settings),
            GetInvitationCollection(database, settings))
    {
    }

    internal TripPlanRepository(
        IMongoCollection<TripPlanDocument> collection,
        TripPlanCreationFingerprint creationFingerprint,
        IMongoCollection<TripParkCandidateDocument>? candidateCollection = null,
        IMongoCollection<TripDayPlanDocument>? dayPlanCollection = null,
        IMongoCollection<TripInvitationDocument>? invitationCollection = null)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.creationFingerprint = creationFingerprint
            ?? throw new ArgumentNullException(nameof(creationFingerprint));
        this.candidateCollection = candidateCollection;
        this.dayPlanCollection = dayPlanCollection;
        this.invitationCollection = invitationCollection;
    }

    public async Task<IdempotentTripPlanCreationResult?> ResolveExistingCreationAsync(
        TripPlan requestedTripPlan,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestedTripPlan);
        IReadOnlyCollection<string> ownerScopeHashes = this.creationFingerprint.HashOwnerScopes(
            requestedTripPlan.OwnerUserId);
        string operationKeyHash = TripPlanCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        TripPlanDocument? existing = await this.collection
            .Find(TripPlanMongoDefinitions.BuildCreationOperationFilter(
                ownerScopeHashes,
                operationKeyHash))
            .FirstOrDefaultAsync(cancellationToken);
        return existing is null
            ? null
            : ResolveIdempotentCreation(
                existing,
                this.creationFingerprint.HashPayload(
                    requestedTripPlan,
                    existing.CreationFingerprintKeyVersion));
    }

    public async Task<IdempotentTripPlanCreationResult> CreateIdempotentAsync(
        TripPlan tripPlan,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        string normalizedOperationId = NormalizeRequired(clientOperationId, nameof(clientOperationId));
        IReadOnlyCollection<string> ownerScopeHashes = this.creationFingerprint.HashOwnerScopes(
            tripPlan.OwnerUserId);
        string currentOwnerScopeHash = this.creationFingerprint.HashOwnerScope(tripPlan.OwnerUserId);
        string operationKeyHash = TripPlanCreationFingerprint.HashOperationKey(normalizedOperationId);
        string currentPayloadHash = this.creationFingerprint.HashPayload(tripPlan);
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        List<TripPlanDocument> owned = await this.collection.Find(
                filters.Eq(static document => document.OwnerUserId, tripPlan.OwnerUserId)
                & filters.Eq(static document => document.DeletionState, TripDeletionState.None))
            .Project<TripPlanDocument>(TripPlanMongoDefinitions.BuildActiveCreationProjection())
            .Limit(TripPlan.MaximumPlansPerOwner)
            .ToListAsync(cancellationToken);
        TripPlanDocument? replay = owned.FirstOrDefault(document => string.Equals(
            document.CreationOperationKeyHash,
            operationKeyHash,
            StringComparison.Ordinal));
        if (replay is not null)
        {
            string replayPayloadHash = this.creationFingerprint.HashPayload(
                tripPlan,
                replay.CreationFingerprintKeyVersion);
            return ResolveIdempotentCreation(replay, replayPayloadHash);
        }

        HashSet<int> occupiedSlots = owned.Select(static document => document.OwnerSlot).ToHashSet();
        for (int ownerSlot = 0; ownerSlot < TripPlan.MaximumPlansPerOwner; ownerSlot++)
        {
            if (occupiedSlots.Contains(ownerSlot))
            {
                continue;
            }

            TripPlanDocument document = tripPlan.ToDocument();
            document.OwnerSlot = ownerSlot;
            document.OwnerScopeHash = currentOwnerScopeHash;
            document.CreationOperationKeyHash = operationKeyHash;
            document.CreationPayloadHash = currentPayloadHash;
            document.CreationFingerprintKeyVersion = this.creationFingerprint.CurrentKeyVersion;
            document.CreationSnapshot = document.CreateCreationSnapshot();
            try
            {
                await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
                return new IdempotentTripPlanCreationResult(
                    IdempotentTripPlanCreationStatus.Created,
                    document.CreationSnapshotToDomain());
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                TripPlanDocument? existing = await this.collection
                    .Find(TripPlanMongoDefinitions.BuildCreationOperationFilter(
                        ownerScopeHashes,
                        operationKeyHash))
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing is not null)
                {
                    string payloadHash = this.creationFingerprint.HashPayload(
                        tripPlan,
                        existing.CreationFingerprintKeyVersion);
                    return ResolveIdempotentCreation(existing, payloadHash);
                }
            }
        }

        return new IdempotentTripPlanCreationResult(
            IdempotentTripPlanCreationStatus.LimitReached,
            null);
    }

    public async Task<IReadOnlyCollection<TripPlan>> ListAccessibleAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> filter = filters.ElemMatch(
                static document => document.Members,
                member => member.UserId == normalizedUserId
                    && member.State == TripMembershipState.Active)
            & filters.Eq(static document => document.DeletionState, TripDeletionState.None);
        FindOptions<TripPlanDocument, TripPlanDocument> options =
            TripPlanMongoDefinitions.BuildAccessibleListOptions();
        using IAsyncCursor<TripPlanDocument> cursor = await this.collection.FindAsync(
            filter,
            options,
            cancellationToken);
        List<TripPlanDocument> documents = await cursor.ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<TripPlan?> GetAccessibleAsync(
        string userId,
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        TripPlanDocument? document = await this.collection.Find(
                filters.Eq(static item => item.Id, tripPlanId.Value)
                & filters.ElemMatch(
                    static item => item.Members,
                    member => member.UserId == normalizedUserId
                        && member.State == TripMembershipState.Active)
                & filters.Eq(static item => item.DeletionState, TripDeletionState.None))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TripPlan?> GetOwnedAsync(
        string userId,
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        TripPlanDocument? document = await this.collection.Find(BuildOwnedFilter(
                normalizedUserId,
                tripPlanId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<long?> GetProgramReadSequenceAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return await this.collection.Find(
                filters.Eq(static document => document.Id, tripPlanId.Value)
                & filters.Eq(static document => document.DeletionState, TripDeletionState.None))
            .Project(static document => (long?)document.ChildMutationLeaseSequence)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TripPlanWriteResult> ReplaceOwnedAsync(
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument> options = new()
        {
            ReturnDocument = ReturnDocument.After,
        };
        TripPlanDocument? persisted = await this.collection.FindOneAndUpdateAsync(
            BuildOwnedFilter(tripPlan.OwnerUserId, tripPlan.Id)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.Version,
                    expectedVersion)
                & TripPlanMongoDefinitions.BuildNoAdmissionInFlightFilter()
                & TripPlanMongoDefinitions.BuildChildEpochMutationFilter(
                    tripPlan.ChildMutationEpoch),
            TripPlanMongoDefinitions.BuildDomainMutation(tripPlan),
            options,
            cancellationToken);
        if (persisted is not null)
        {
            TripPlan persistedTripPlan = persisted.ToDomain();
            return new TripPlanWriteResult(
                TripPlanWriteOutcome.Success,
                persistedTripPlan.Version,
                persistedTripPlan);
        }

        TripPlan? existing = await this.GetOwnedAsync(
            tripPlan.OwnerUserId,
            tripPlan.Id,
            cancellationToken);
        return existing is null
            ? new TripPlanWriteResult(TripPlanWriteOutcome.NotFound, null)
            : new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, existing.Version);
    }

    public async Task<TripPlanWriteResult> ReplaceAccessibleAsync(
        string actorUserId,
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        string normalizedActorUserId = IdentifierRules.NormalizeRequired(actorUserId, nameof(actorUserId));
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        TripPlanDocument? persisted = await this.collection.FindOneAndUpdateAsync(
            filters.Eq(static document => document.Id, tripPlan.Id.Value)
            & filters.Eq(static document => document.Version, expectedVersion)
            & filters.Eq(static document => document.DeletionState, TripDeletionState.None)
            & TripPlanMongoDefinitions.BuildNoAdmissionInFlightFilter()
            & TripPlanMongoDefinitions.BuildChildEpochMutationFilter(
                tripPlan.ChildMutationEpoch)
            & filters.ElemMatch(
                static document => document.Members,
                member => member.UserId == normalizedActorUserId
                    && member.State == TripMembershipState.Active),
            TripPlanMongoDefinitions.BuildDomainMutation(tripPlan),
            new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (persisted is not null)
        {
            TripPlan current = persisted.ToDomain();
            return new TripPlanWriteResult(TripPlanWriteOutcome.Success, current.Version, current);
        }

        TripPlan? existing = await this.GetAccessibleAsync(normalizedActorUserId, tripPlan.Id, cancellationToken);
        return existing is null
            ? new TripPlanWriteResult(TripPlanWriteOutcome.NotFound, null)
            : new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, existing.Version);
    }

    public async Task<TripPlanWriteResult> TransferOwnershipAsync(
        string previousOwnerUserId,
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        string normalizedPreviousOwner = IdentifierRules.NormalizeRequired(
            previousOwnerUserId,
            nameof(previousOwnerUserId));
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        HashSet<int> occupiedSlots = (await this.collection.Find(
                filters.Eq(static document => document.OwnerUserId, tripPlan.OwnerUserId)
                & filters.Eq(static document => document.DeletionState, TripDeletionState.None))
            .Project(static document => document.OwnerSlot)
            .Limit(TripPlan.MaximumPlansPerOwner)
            .ToListAsync(cancellationToken)).ToHashSet();
        TripPlanDocument domain = tripPlan.ToDocument();
        for (int ownerSlot = 0; ownerSlot < TripPlan.MaximumPlansPerOwner; ownerSlot++)
        {
            if (occupiedSlots.Contains(ownerSlot))
            {
                continue;
            }

            try
            {
                TripPlanDocument? persisted = await this.collection.FindOneAndUpdateAsync(
                    filters.Eq(static document => document.Id, tripPlan.Id.Value)
                    & filters.Eq(static document => document.OwnerUserId, normalizedPreviousOwner)
                    & filters.Eq(static document => document.Version, expectedVersion)
                    & filters.Eq(static document => document.DeletionState, TripDeletionState.None)
                    & TripPlanMongoDefinitions.BuildNoAdmissionInFlightFilter()
                    & TripPlanMongoDefinitions.BuildChildEpochMutationFilter(
                        tripPlan.ChildMutationEpoch),
                    TripPlanMongoDefinitions.BuildDomainMutation(tripPlan)
                        .Set(static document => document.OwnerUserId, tripPlan.OwnerUserId)
                        .Set(static document => document.OwnerSlot, ownerSlot)
                        .Set(
                            static document => document.OwnerScopeHash,
                            this.creationFingerprint.HashOwnerScope(tripPlan.OwnerUserId)),
                    new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
                    {
                        ReturnDocument = ReturnDocument.After,
                    },
                    cancellationToken);
                if (persisted is not null)
                {
                    TripPlan current = persisted.ToDomain();
                    return new TripPlanWriteResult(TripPlanWriteOutcome.Success, current.Version, current);
                }
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                occupiedSlots.Add(ownerSlot);
                continue;
            }

            break;
        }

        TripPlanDocument? currentDocument = await this.collection.Find(
                filters.Eq(static document => document.Id, tripPlan.Id.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return currentDocument is null
            ? new TripPlanWriteResult(TripPlanWriteOutcome.NotFound, null)
            : new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, currentDocument.Version);
    }

    public async Task<TripPlanWriteResult> DeleteOwnedAsync(
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        if (expectedVersion == long.MaxValue || tripPlan.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The deleted trip must be exactly one version ahead of the expected version.",
                nameof(tripPlan));
        }

        UpdateResult result = await this.collection.UpdateOneAsync(
            BuildOwnedFilter(tripPlan.OwnerUserId, tripPlan.Id)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.Version,
                    expectedVersion)
                & TripPlanMongoDefinitions.BuildNoAdmissionInFlightFilter()
                & TripPlanMongoDefinitions.BuildNoActiveChildLeaseFilter(),
            TripPlanMongoDefinitions.BuildDomainMutation(tripPlan),
            cancellationToken: cancellationToken);
        if (result.MatchedCount == 1)
        {
            return new TripPlanWriteResult(TripPlanWriteOutcome.Success, tripPlan.Version);
        }

        TripPlan? existing = await this.GetOwnedAsync(
            tripPlan.OwnerUserId,
            tripPlan.Id,
            cancellationToken);
        return existing is null
            ? new TripPlanWriteResult(TripPlanWriteOutcome.NotFound, null)
            : new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, existing.Version);
    }

    public async Task<TripPlanWriteResult> ReplaceOwnedUnderChildLeaseAsync(
        TripPlan tripPlan,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        ArgumentNullException.ThrowIfNull(lease);
        if (lease.ChildMutationEpoch == long.MaxValue
            || tripPlan.ChildMutationEpoch != lease.ChildMutationEpoch + 1
            || expectedVersion == long.MaxValue
            || tripPlan.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The root mutation must advance exactly one version and one child epoch.",
                nameof(tripPlan));
        }

        UpdateDefinitionBuilder<TripPlanDocument> updates = Builders<TripPlanDocument>.Update;
        UpdateDefinition<TripPlanDocument> update = updates.Combine(
            TripPlanMongoDefinitions.BuildDomainMutation(tripPlan),
            updates.Set(
                static document => document.ActiveChildMutationLeases,
                new List<TripChildMutationLeaseDocument>()));
        FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument> options = new()
        {
            ReturnDocument = ReturnDocument.After,
        };
        TripPlanDocument? persisted = await this.collection.FindOneAndUpdateAsync(
            BuildOwnedFilter(tripPlan.OwnerUserId, tripPlan.Id)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.Version,
                    expectedVersion)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.ChildMutationEpoch,
                    lease.ChildMutationEpoch)
                & TripPlanMongoDefinitions.BuildNoAdmissionInFlightFilter()
                & TripPlanMongoDefinitions.BuildActiveChildLeaseIdentityFilter(lease),
            update,
            options,
            cancellationToken);
        if (persisted is not null)
        {
            TripPlan persistedTripPlan = persisted.ToDomain();
            return new TripPlanWriteResult(
                TripPlanWriteOutcome.Success,
                persistedTripPlan.Version,
                persistedTripPlan);
        }

        TripPlan? existing = await this.GetOwnedAsync(
            tripPlan.OwnerUserId,
            tripPlan.Id,
            cancellationToken);
        return existing is null
            ? new TripPlanWriteResult(TripPlanWriteOutcome.NotFound, null)
            : new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, existing.Version);
    }

    public async Task<TripPlanWriteResult> ReplaceAccessibleUnderChildLeaseAsync(
        string actorUserId,
        TripPlan tripPlan,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        ArgumentNullException.ThrowIfNull(lease);
        string normalizedActorUserId = IdentifierRules.NormalizeRequired(actorUserId, nameof(actorUserId));
        if (lease.ChildMutationEpoch == long.MaxValue
            || tripPlan.ChildMutationEpoch != lease.ChildMutationEpoch + 1
            || expectedVersion == long.MaxValue
            || tripPlan.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The root mutation must advance exactly one version and one child epoch.",
                nameof(tripPlan));
        }

        UpdateDefinitionBuilder<TripPlanDocument> updates = Builders<TripPlanDocument>.Update;
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        TripPlanDocument? persisted = await this.collection.FindOneAndUpdateAsync(
            filters.Eq(static document => document.Id, tripPlan.Id.Value)
            & filters.Eq(static document => document.Version, expectedVersion)
            & filters.Eq(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
            & TripPlanMongoDefinitions.BuildNoAdmissionInFlightFilter()
            & filters.ElemMatch(
                static document => document.Members,
                member => member.UserId == normalizedActorUserId
                    && member.State == TripMembershipState.Active)
            & TripPlanMongoDefinitions.BuildActiveChildLeaseIdentityFilter(lease),
            updates.Combine(
                TripPlanMongoDefinitions.BuildDomainMutation(tripPlan),
                updates.Set(
                    static document => document.ActiveChildMutationLeases,
                    new List<TripChildMutationLeaseDocument>())),
            new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (persisted is not null)
        {
            TripPlan current = persisted.ToDomain();
            return new TripPlanWriteResult(TripPlanWriteOutcome.Success, current.Version, current);
        }

        TripPlan? existing = await this.GetAccessibleAsync(normalizedActorUserId, tripPlan.Id, cancellationToken);
        return existing is null
            ? new TripPlanWriteResult(TripPlanWriteOutcome.NotFound, null)
            : new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, existing.Version);
    }

    public async Task PurgeChildrenAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        if (this.candidateCollection is null
            || this.dayPlanCollection is null
            || this.invitationCollection is null)
        {
            throw new InvalidOperationException("Trip child collections are required to purge a trip.");
        }

        DeleteResult candidates = await this.candidateCollection.DeleteManyAsync(
            Builders<TripParkCandidateDocument>.Filter.Eq(
                static document => document.TripPlanId,
                tripPlanId.Value),
            cancellationToken);
        DeleteResult days = await this.dayPlanCollection.DeleteManyAsync(
            Builders<TripDayPlanDocument>.Filter.Eq(
                static document => document.TripPlanId,
                tripPlanId.Value),
            cancellationToken);
        DeleteResult invitations = await this.invitationCollection.DeleteManyAsync(
            Builders<TripInvitationDocument>.Filter.Eq(
                static document => document.TripPlanId,
                tripPlanId.Value),
            cancellationToken);
        _ = candidates.DeletedCount;
        _ = days.DeletedCount;
        _ = invitations.DeletedCount;
    }

    public async Task<TripPlanWriteResult> FinalizeDeletionOwnedAsync(
        TripPlan tripPlan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        UpdateResult result = await this.collection.UpdateOneAsync(
            TripPlanMongoDefinitions.BuildDeletionFinalizationFilter(tripPlan),
            TripPlanMongoDefinitions.BuildDeletionTombstone(tripPlan),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1
            ? new TripPlanWriteResult(TripPlanWriteOutcome.Success, tripPlan.Version)
            : new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, null);
    }

    public async Task<IReadOnlyCollection<TripPlan>> ListPendingDeletionAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        List<TripPlanDocument> documents = await this.collection.Find(
                Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.DeletionState,
                    TripDeletionState.Pending))
            .SortBy(static document => document.UpdatedAt)
            .ThenBy(static document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    internal static IdempotentTripPlanCreationResult ResolveIdempotentCreation(
        TripPlanDocument existing,
        string payloadHash)
    {
        ArgumentNullException.ThrowIfNull(existing);
        bool matches = !string.IsNullOrWhiteSpace(existing.CreationPayloadHash)
            && string.Equals(existing.CreationPayloadHash, payloadHash, StringComparison.Ordinal);
        if (!matches)
        {
            return new IdempotentTripPlanCreationResult(
                IdempotentTripPlanCreationStatus.Conflict,
                null);
        }

        if (existing.DeletionState != TripDeletionState.None)
        {
            return new IdempotentTripPlanCreationResult(
                IdempotentTripPlanCreationStatus.Deleted,
                null);
        }

        return new IdempotentTripPlanCreationResult(
            IdempotentTripPlanCreationStatus.Replayed,
            existing.CreationSnapshotToDomain());
    }

    private static FilterDefinition<TripPlanDocument> BuildOwnedFilter(
        string ownerUserId,
        TripPlanId tripPlanId)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return filters.Eq(static document => document.Id, tripPlanId.Value)
            & filters.Eq(static document => document.OwnerUserId, ownerUserId)
            & filters.Eq(static document => document.DeletionState, TripDeletionState.None);
    }

    private static IMongoCollection<TripPlanDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<TripPlanDocument>(settings.TripPlansCollectionName);
    }

    private static IMongoCollection<TripParkCandidateDocument> GetCandidateCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        return database.GetCollection<TripParkCandidateDocument>(settings.TripParkCandidatesCollectionName);
    }

    private static IMongoCollection<TripDayPlanDocument> GetDayPlanCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        return database.GetCollection<TripDayPlanDocument>(settings.TripDayPlansCollectionName);
    }

    private static IMongoCollection<TripInvitationDocument> GetInvitationCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        return database.GetCollection<TripInvitationDocument>(settings.TripInvitationsCollectionName);
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
