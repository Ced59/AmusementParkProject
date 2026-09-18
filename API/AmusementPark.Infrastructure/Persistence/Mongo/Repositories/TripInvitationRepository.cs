using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripInvitationRepository : ITripInvitationRepository
{
    private static readonly TimeSpan PreparedRetention = TimeSpan.FromMinutes(5);
    private readonly IMongoCollection<TripInvitationDocument> collection;

    public TripInvitationRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripInvitationDocument>(
            settings.TripInvitationsCollectionName);
    }

    internal TripInvitationRepository(IMongoCollection<TripInvitationDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    internal static IReadOnlyCollection<CreateIndexModel<TripInvitationDocument>> BuildIndexes()
    {
        FilterDefinitionBuilder<TripInvitationDocument> filters =
            Builders<TripInvitationDocument>.Filter;
        return new List<CreateIndexModel<TripInvitationDocument>>
        {
            new(
                Builders<TripInvitationDocument>.IndexKeys.Ascending(
                    static document => document.TokenHash),
                new CreateIndexOptions<TripInvitationDocument>
                {
                    Unique = true,
                    Name = "uq_trip_invitation_token_hash",
                }),
            new(
                Builders<TripInvitationDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.InviterMemberId)
                    .Ascending(static document => document.OperationKeyHash),
                new CreateIndexOptions<TripInvitationDocument>
                {
                    Unique = true,
                    Name = "uq_trip_invitation_operation",
                }),
            new(
                Builders<TripInvitationDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.ActiveSlot),
                new CreateIndexOptions<TripInvitationDocument>
                {
                    Unique = true,
                    Name = "uq_trip_invitation_active_slot",
                    PartialFilterExpression = filters.Type(
                        static document => document.ActiveSlot,
                        BsonType.Int32),
                }),
            new(
                Builders<TripInvitationDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "ix_trip_invitation_plan_status_created" }),
            new(
                Builders<TripInvitationDocument>.IndexKeys
                    .Ascending(static document => document.Status)
                    .Ascending(static document => document.ExpiresAtUtc),
                new CreateIndexOptions { Name = "ix_trip_invitation_status_expires" }),
            new(
                Builders<TripInvitationDocument>.IndexKeys
                    .Ascending(static document => document.Status)
                    .Ascending(static document => document.AcceptanceLeaseExpiresAtUtc)
                    .Ascending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "ix_trip_invitation_acceptance_recovery" }),
            new(
                Builders<TripInvitationDocument>.IndexKeys.Ascending(
                    static document => document.RetentionExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "ttl_trip_invitation_retention",
                    ExpireAfter = TimeSpan.Zero,
                }),
            new(
                Builders<TripInvitationDocument>.IndexKeys.Ascending(
                    static document => document.ReservedExpiresAtUtc),
                new CreateIndexOptions<TripInvitationDocument>
                {
                    Name = "ttl_trip_invitation_prepared",
                    ExpireAfter = TimeSpan.Zero,
                    PartialFilterExpression = filters.Eq(
                        static document => document.Status,
                        TripInvitationStatus.Prepared),
                }),
        };
    }

    public async Task<TripInvitationCreationRecord?> ResolveCreationAsync(
        TripPlanId tripPlanId,
        TripMemberId inviterMemberId,
        string operationKeyHash,
        CancellationToken cancellationToken)
    {
        TripInvitationDocument? document = await this.collection.Find(
                BuildOperationFilter(tripPlanId, inviterMemberId, operationKeyHash))
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToCreationRecord(document, true);
    }

    public async Task<TripInvitationCreationWriteResult> CreateAsync(
        TripInvitation invitation,
        TripChildMutationLease lease,
        string operationKeyHash,
        string requestHash,
        string sealedToken,
        string sealedTokenKeyVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        ArgumentNullException.ThrowIfNull(lease);
        string normalizedOperationHash = NormalizeRequired(operationKeyHash, nameof(operationKeyHash));
        string normalizedRequestHash = NormalizeRequired(requestHash, nameof(requestHash));
        string normalizedSealedToken = NormalizeRequired(sealedToken, nameof(sealedToken));
        string normalizedKeyVersion = NormalizeRequired(sealedTokenKeyVersion, nameof(sealedTokenKeyVersion));

        await this.ExpireElapsedInvitationsAsync(invitation.TripPlanId, cancellationToken);

        TripInvitationDocument? existing = await this.collection.Find(BuildOperationFilter(
                invitation.TripPlanId,
                invitation.InviterMemberId,
                normalizedOperationHash))
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return await this.ResumeOrReplayAsync(
                existing,
                lease,
                normalizedRequestHash,
                cancellationToken);
        }

        HashSet<int> occupiedSlots = (await this.collection.Find(
                Builders<TripInvitationDocument>.Filter.Eq(
                    static document => document.TripPlanId,
                    invitation.TripPlanId.Value)
                & Builders<TripInvitationDocument>.Filter.Exists(
                    static document => document.ActiveSlot,
                    true))
            .Project(static document => document.ActiveSlot)
            .ToListAsync(cancellationToken))
            .Where(static slot => slot.HasValue)
            .Select(static slot => slot!.Value)
            .ToHashSet();

        for (int slot = 0; slot < TripInvitation.MaximumActiveInvitationsPerTrip; slot++)
        {
            if (occupiedSlots.Contains(slot))
            {
                continue;
            }

            TripInvitationDocument shell = invitation.ToDocument();
            shell.Status = TripInvitationStatus.Prepared;
            shell.OperationKeyHash = normalizedOperationHash;
            shell.RequestHash = normalizedRequestHash;
            shell.SealedToken = normalizedSealedToken;
            shell.SealedTokenKeyVersion = normalizedKeyVersion;
            shell.ChildMutationEpoch = lease.ChildMutationEpoch;
            shell.LeaseGeneration = lease.Generation;
            shell.LeaseExpiresAtUtc = lease.ExpiresAtUtc;
            shell.ActiveSlot = slot;
            shell.ReservedExpiresAtUtc = lease.ExpiresAtUtc.Add(PreparedRetention);
            try
            {
                await this.collection.InsertOneAsync(shell, cancellationToken: cancellationToken);
                return await this.ActivateAsync(shell, lease, cancellationToken);
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                TripInvitationDocument? operationReplay = await this.collection.Find(
                        BuildOperationFilter(
                            invitation.TripPlanId,
                            invitation.InviterMemberId,
                            normalizedOperationHash))
                    .FirstOrDefaultAsync(cancellationToken);
                if (operationReplay is not null)
                {
                    return await this.ResumeOrReplayAsync(
                        operationReplay,
                        lease,
                        normalizedRequestHash,
                        cancellationToken);
                }

                bool tokenCollision = await this.collection.Find(
                        Builders<TripInvitationDocument>.Filter.Eq(
                            static document => document.TokenHash,
                            invitation.TokenHash))
                    .AnyAsync(cancellationToken);
                if (tokenCollision)
                {
                    return new TripInvitationCreationWriteResult(
                        TripInvitationCreationWriteOutcome.TokenCollision);
                }

                occupiedSlots.Add(slot);
            }
        }

        return new TripInvitationCreationWriteResult(
            TripInvitationCreationWriteOutcome.LimitReached);
    }

    public async Task<IReadOnlyCollection<TripInvitation>> ListActiveAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        List<TripInvitationDocument> documents = await this.collection.Find(
                BuildPublicActiveFilter()
                & Builders<TripInvitationDocument>.Filter.Eq(
                    static document => document.TripPlanId,
                    tripPlanId.Value))
            .SortByDescending(static document => document.CreatedAt)
            .Limit(TripInvitation.MaximumActiveInvitationsPerTrip)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<int> ExpireElapsedAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        FilterDefinitionBuilder<TripInvitationDocument> filters =
            Builders<TripInvitationDocument>.Filter;
        FilterDefinition<TripInvitationDocument> elapsedFilter =
            filters.Eq(static document => document.Status, TripInvitationStatus.Active)
            & BuildElapsedExpirationFilter();
        List<string> invitationIds = await this.collection.Find(elapsedFilter)
            .SortBy(static document => document.ExpiresAtUtc)
            .Project(static document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        if (invitationIds.Count == 0)
        {
            return 0;
        }

        UpdateResult result = await this.collection.UpdateManyAsync(
            filters.In(static document => document.Id, invitationIds)
            & elapsedFilter,
            Builders<TripInvitationDocument>.Update
                .Set(static document => document.Status, TripInvitationStatus.Expired)
                .CurrentDate(static document => document.UpdatedAt)
                .Unset(static document => document.ActiveSlot)
                .Unset(static document => document.SealedToken)
                .Unset(static document => document.SealedTokenKeyVersion),
            cancellationToken: cancellationToken);
        return checked((int)result.ModifiedCount);
    }

    public async Task<TripInvitation?> GetOwnedAsync(
        TripPlanId tripPlanId,
        TripInvitationId invitationId,
        CancellationToken cancellationToken)
    {
        TripInvitationDocument? document = await this.collection.Find(
                Builders<TripInvitationDocument>.Filter.Eq(
                    static item => item.TripPlanId,
                    tripPlanId.Value)
                & Builders<TripInvitationDocument>.Filter.Eq(
                    static item => item.Id,
                    invitationId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TripInvitation?> GetPublicByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        string normalizedTokenHash = NormalizeRequired(tokenHash, nameof(tokenHash));
        TripInvitationDocument? document = await this.collection.Find(
                BuildPublicActiveFilter()
                & Builders<TripInvitationDocument>.Filter.Eq(
                    static item => item.TokenHash,
                    normalizedTokenHash))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TripInvitationWriteOutcome> RevokeAsync(
        TripInvitation invitation,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        ArgumentNullException.ThrowIfNull(lease);
        if (expectedVersion == long.MaxValue || invitation.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The revoked invitation must be exactly one version ahead of the expected version.",
                nameof(invitation));
        }

        UpdateDefinitionBuilder<TripInvitationDocument> updates =
            Builders<TripInvitationDocument>.Update;
        UpdateResult result = await this.collection.UpdateOneAsync(
            Builders<TripInvitationDocument>.Filter.Eq(
                static document => document.Id,
                invitation.Id.Value)
            & Builders<TripInvitationDocument>.Filter.Eq(
                static document => document.TripPlanId,
                invitation.TripPlanId.Value)
            & Builders<TripInvitationDocument>.Filter.Eq(
                static document => document.Status,
                TripInvitationStatus.Active)
            & Builders<TripInvitationDocument>.Filter.Eq(
                static document => document.Version,
                expectedVersion)
            & BuildLeaseTimeGuard(lease),
            updates.Set(static document => document.Status, TripInvitationStatus.Revoked)
                .Set(static document => document.RevokedAtUtc, invitation.RevokedAtUtc)
                .Set(static document => document.UpdatedAt, invitation.UpdatedAtUtc)
                .Set(static document => document.Version, invitation.Version)
                .Unset(static document => document.ActiveSlot)
                .Unset(static document => document.SealedToken)
                .Unset(static document => document.SealedTokenKeyVersion),
            cancellationToken: cancellationToken);
        if (result.ModifiedCount == 1)
        {
            return TripInvitationWriteOutcome.Success;
        }

        TripInvitationDocument? current = await this.collection.Find(
                Builders<TripInvitationDocument>.Filter.Eq(
                    static document => document.Id,
                    invitation.Id.Value)
                & Builders<TripInvitationDocument>.Filter.Eq(
                    static document => document.TripPlanId,
                    invitation.TripPlanId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return current is null
            ? TripInvitationWriteOutcome.NotFound
            : current.Status == TripInvitationStatus.Revoked
                ? TripInvitationWriteOutcome.Success
                : TripInvitationWriteOutcome.Conflict;
    }

    public Task PurgeAsync(TripPlanId tripPlanId, CancellationToken cancellationToken)
    {
        return this.collection.DeleteManyAsync(
            Builders<TripInvitationDocument>.Filter.Eq(
                static document => document.TripPlanId,
                tripPlanId.Value),
            cancellationToken);
    }

    private async Task<TripInvitationCreationWriteResult> ResumeOrReplayAsync(
        TripInvitationDocument existing,
        TripChildMutationLease lease,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return new TripInvitationCreationWriteResult(
                TripInvitationCreationWriteOutcome.IdempotencyConflict);
        }

        if (existing.Status == TripInvitationStatus.Active)
        {
            return Success(existing, true);
        }

        if (existing.Status != TripInvitationStatus.Prepared)
        {
            return new TripInvitationCreationWriteResult(
                TripInvitationCreationWriteOutcome.IdempotencyConflict);
        }

        FilterDefinitionBuilder<TripInvitationDocument> filters =
            Builders<TripInvitationDocument>.Filter;
        FilterDefinition<TripInvitationDocument> sameLease = filters.Eq(
                static document => document.ChildMutationEpoch,
                lease.ChildMutationEpoch)
            & filters.Eq(static document => document.LeaseGeneration, lease.Generation);
        TripInvitationDocument? reclaimed = await this.collection.FindOneAndUpdateAsync(
            filters.Eq(static document => document.Id, existing.Id)
            & filters.Eq(static document => document.Status, TripInvitationStatus.Prepared)
            & filters.Eq(static document => document.RequestHash, requestHash)
            & (sameLease | TripChildMutationMongoDefinitions.BuildExpiredCreationLeaseGuard<TripInvitationDocument>()),
            Builders<TripInvitationDocument>.Update
                .Set(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
                .Set(static document => document.LeaseGeneration, lease.Generation)
                .Set(static document => document.LeaseExpiresAtUtc, lease.ExpiresAtUtc)
                .Set(
                    static document => document.ReservedExpiresAtUtc,
                    lease.ExpiresAtUtc.Add(PreparedRetention)),
            new FindOneAndUpdateOptions<TripInvitationDocument, TripInvitationDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        return reclaimed is null
            ? new TripInvitationCreationWriteResult(TripInvitationCreationWriteOutcome.LeaseExpired)
            : await this.ActivateAsync(reclaimed, lease, cancellationToken);
    }

    private async Task ExpireElapsedInvitationsAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripInvitationDocument> filters =
            Builders<TripInvitationDocument>.Filter;
        await this.collection.UpdateManyAsync(
            filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & filters.Eq(static document => document.Status, TripInvitationStatus.Active)
            & BuildElapsedExpirationFilter(),
            Builders<TripInvitationDocument>.Update
                .Set(static document => document.Status, TripInvitationStatus.Expired)
                .CurrentDate(static document => document.UpdatedAt)
                .Unset(static document => document.ActiveSlot)
                .Unset(static document => document.SealedToken)
                .Unset(static document => document.SealedTokenKeyVersion),
            cancellationToken: cancellationToken);
    }

    private async Task<TripInvitationCreationWriteResult> ActivateAsync(
        TripInvitationDocument shell,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripInvitationDocument> filters =
            Builders<TripInvitationDocument>.Filter;
        TripInvitationDocument? active = await this.collection.FindOneAndUpdateAsync(
            filters.Eq(static document => document.Id, shell.Id)
            & filters.Eq(static document => document.Status, TripInvitationStatus.Prepared)
            & filters.Eq(static document => document.OperationKeyHash, shell.OperationKeyHash)
            & filters.Eq(static document => document.RequestHash, shell.RequestHash)
            & filters.Eq(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
            & filters.Eq(static document => document.LeaseGeneration, lease.Generation)
            & TripChildMutationMongoDefinitions.BuildCreationLeaseGuard<TripInvitationDocument>(),
            Builders<TripInvitationDocument>.Update
                .Set(static document => document.Status, TripInvitationStatus.Active)
                .Unset(static document => document.ReservedExpiresAtUtc),
            new FindOneAndUpdateOptions<TripInvitationDocument, TripInvitationDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (active is not null)
        {
            return Success(active, false);
        }

        TripInvitationDocument? replay = await this.collection.Find(
                filters.Eq(static document => document.Id, shell.Id)
                & filters.Eq(static document => document.Status, TripInvitationStatus.Active)
                & filters.Eq(static document => document.OperationKeyHash, shell.OperationKeyHash)
                & filters.Eq(static document => document.RequestHash, shell.RequestHash))
            .FirstOrDefaultAsync(cancellationToken);
        return replay is null
            ? new TripInvitationCreationWriteResult(TripInvitationCreationWriteOutcome.LeaseExpired)
            : Success(replay, true);
    }

    private static TripInvitationCreationWriteResult Success(
        TripInvitationDocument document,
        bool wasReplayed)
    {
        return new TripInvitationCreationWriteResult(
            TripInvitationCreationWriteOutcome.Success,
            ToCreationRecord(document, wasReplayed));
    }

    private static TripInvitationCreationRecord ToCreationRecord(
        TripInvitationDocument document,
        bool wasReplayed)
    {
        return new TripInvitationCreationRecord(
            document.ToDomain(),
            document.OperationKeyHash,
            document.RequestHash,
            document.SealedToken ?? string.Empty,
            document.SealedTokenKeyVersion ?? string.Empty,
            wasReplayed);
    }

    private static FilterDefinition<TripInvitationDocument> BuildOperationFilter(
        TripPlanId tripPlanId,
        TripMemberId inviterMemberId,
        string operationKeyHash)
    {
        FilterDefinitionBuilder<TripInvitationDocument> filters =
            Builders<TripInvitationDocument>.Filter;
        return filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & filters.Eq(static document => document.InviterMemberId, inviterMemberId.Value)
            & filters.Eq(
                static document => document.OperationKeyHash,
                NormalizeRequired(operationKeyHash, nameof(operationKeyHash)));
    }

    internal static FilterDefinition<TripInvitationDocument> BuildPublicActiveFilter()
    {
        return Builders<TripInvitationDocument>.Filter.Eq(
                static document => document.Status,
                TripInvitationStatus.Active)
            & new BsonDocumentFilterDefinition<TripInvitationDocument>(new BsonDocument(
                "$expr",
                new BsonDocument("$lt", new BsonArray { "$$NOW", "$expiresAtUtc" })));
    }

    internal static FilterDefinition<TripInvitationDocument> BuildElapsedExpirationFilter()
    {
        return new BsonDocumentFilterDefinition<TripInvitationDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$gte", new BsonArray { "$$NOW", "$expiresAtUtc" })));
    }

    private static FilterDefinition<TripInvitationDocument> BuildLeaseTimeGuard(
        TripChildMutationLease lease)
    {
        return new BsonDocumentFilterDefinition<TripInvitationDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$lt", new BsonArray
            {
                "$$NOW",
                new BsonDateTime(lease.ExpiresAtUtc),
            })));
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("A required invitation persistence value is missing.", parameterName);
    }
}
