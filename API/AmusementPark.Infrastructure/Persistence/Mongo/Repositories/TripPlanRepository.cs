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

    public TripPlanRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal TripPlanRepository(IMongoCollection<TripPlanDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<IdempotentTripPlanCreationResult?> ResolveExistingCreationAsync(
        TripPlan requestedTripPlan,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestedTripPlan);
        string operationKeyHash = TripPlanCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        TripPlanDocument? existing = await this.collection
            .Find(TripPlanMongoDefinitions.BuildCreationOperationFilter(
                requestedTripPlan.OwnerUserId,
                operationKeyHash))
            .FirstOrDefaultAsync(cancellationToken);
        return existing is null
            ? null
            : ResolveIdempotentCreation(existing, TripPlanCreationFingerprint.HashPayload(requestedTripPlan));
    }

    public async Task<IdempotentTripPlanCreationResult> CreateIdempotentAsync(
        TripPlan tripPlan,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        string normalizedOperationId = NormalizeRequired(clientOperationId, nameof(clientOperationId));
        string operationKeyHash = TripPlanCreationFingerprint.HashOperationKey(normalizedOperationId);
        string payloadHash = TripPlanCreationFingerprint.HashPayload(tripPlan);
        List<TripPlanDocument> owned = await this.collection.Find(
                Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.OwnerUserId,
                    tripPlan.OwnerUserId))
            .Project<TripPlanDocument>(Builders<TripPlanDocument>.Projection
                .Include(static document => document.OwnerSlot)
                .Include(static document => document.CreationOperationKeyHash)
                .Include(static document => document.CreationPayloadHash)
                .Include(static document => document.CreationSnapshot)
                .Include(static document => document.Id)
                .Include(static document => document.OwnerUserId))
            .Limit(TripPlan.MaximumPlansPerOwner)
            .ToListAsync(cancellationToken);
        TripPlanDocument? replay = owned.FirstOrDefault(document => string.Equals(
            document.CreationOperationKeyHash,
            operationKeyHash,
            StringComparison.Ordinal));
        if (replay is not null)
        {
            return ResolveIdempotentCreation(replay, payloadHash);
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
            document.CreationOperationKeyHash = operationKeyHash;
            document.CreationPayloadHash = payloadHash;
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
                        tripPlan.OwnerUserId,
                        operationKeyHash))
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing is not null)
                {
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

    public async Task<TripPlanWriteOutcome> ReplaceOwnedAsync(
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        UpdateResult result = await this.collection.UpdateOneAsync(
            BuildOwnedFilter(tripPlan.OwnerUserId, tripPlan.Id)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.Version,
                    expectedVersion),
            TripPlanMongoDefinitions.BuildDomainMutation(tripPlan),
            cancellationToken: cancellationToken);
        if (result.MatchedCount == 1)
        {
            return TripPlanWriteOutcome.Success;
        }

        TripPlan? existing = await this.GetOwnedAsync(
            tripPlan.OwnerUserId,
            tripPlan.Id,
            cancellationToken);
        return existing is null ? TripPlanWriteOutcome.NotFound : TripPlanWriteOutcome.Conflict;
    }

    public async Task<TripPlanWriteOutcome> DeleteOwnedAsync(
        string userId,
        TripPlanId tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        DeleteResult result = await this.collection.DeleteOneAsync(
            BuildOwnedFilter(normalizedUserId, tripPlanId)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.Version,
                    expectedVersion),
            cancellationToken);
        if (result.DeletedCount == 1)
        {
            return TripPlanWriteOutcome.Success;
        }

        TripPlan? existing = await this.GetOwnedAsync(normalizedUserId, tripPlanId, cancellationToken);
        return existing is null ? TripPlanWriteOutcome.NotFound : TripPlanWriteOutcome.Conflict;
    }

    internal static IdempotentTripPlanCreationResult ResolveIdempotentCreation(
        TripPlanDocument existing,
        string payloadHash)
    {
        ArgumentNullException.ThrowIfNull(existing);
        bool matches = !string.IsNullOrWhiteSpace(existing.CreationPayloadHash)
            && string.Equals(existing.CreationPayloadHash, payloadHash, StringComparison.Ordinal);
        return matches
            ? new IdempotentTripPlanCreationResult(
                IdempotentTripPlanCreationStatus.Replayed,
                existing.CreationSnapshotToDomain())
            : new IdempotentTripPlanCreationResult(
                IdempotentTripPlanCreationStatus.Conflict,
                null);
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
