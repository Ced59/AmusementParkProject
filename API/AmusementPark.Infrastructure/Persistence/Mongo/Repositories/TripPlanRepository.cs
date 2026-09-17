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
    private readonly TripPlanCreationFingerprint creationFingerprint;

    public TripPlanRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        TripPlanCreationFingerprint creationFingerprint)
        : this(
            GetCollection(database, settings),
            creationFingerprint,
            GetCandidateCollection(database, settings),
            GetDayPlanCollection(database, settings))
    {
    }

    internal TripPlanRepository(
        IMongoCollection<TripPlanDocument> collection,
        TripPlanCreationFingerprint creationFingerprint,
        IMongoCollection<TripParkCandidateDocument>? candidateCollection = null,
        IMongoCollection<TripDayPlanDocument>? dayPlanCollection = null)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.creationFingerprint = creationFingerprint
            ?? throw new ArgumentNullException(nameof(creationFingerprint));
        this.candidateCollection = candidateCollection;
        this.dayPlanCollection = dayPlanCollection;
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
        List<TripPlanDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.UpdatedAt)
            .ThenBy(static document => document.Id)
            .Limit(TripPlan.MaximumPlansPerOwner)
            .ToListAsync(cancellationToken);
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

    public async Task PurgeChildrenAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        if (this.candidateCollection is null || this.dayPlanCollection is null)
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
        _ = candidates.DeletedCount;
        _ = days.DeletedCount;
    }

    public async Task<TripPlanWriteResult> FinalizeDeletionOwnedAsync(
        TripPlan tripPlan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        UpdateResult result = await this.collection.UpdateOneAsync(
            filters.Eq(static document => document.Id, tripPlan.Id.Value)
                & filters.Eq(static document => document.OwnerUserId, tripPlan.OwnerUserId)
                & filters.Eq(static document => document.DeletionState, TripDeletionState.Pending)
                & filters.Eq(static document => document.Version, tripPlan.Version),
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
