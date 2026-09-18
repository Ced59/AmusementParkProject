using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripAdmissionRepository : ITripAdmissionRepository
{
    private const int AdmissionLeaseSeconds = 120;
    private readonly IMongoCollection<TripPlanDocument> plans;
    private readonly IMongoCollection<TripInvitationDocument> invitations;

    public TripAdmissionRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.plans = database.GetCollection<TripPlanDocument>(settings.TripPlansCollectionName);
        this.invitations = database.GetCollection<TripInvitationDocument>(settings.TripInvitationsCollectionName);
    }

    public async Task<TripInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        string normalizedHash = NormalizeRequired(tokenHash, nameof(tokenHash));
        TripInvitationDocument? document = await this.invitations.Find(
                Builders<TripInvitationDocument>.Filter.Eq(
                    static invitation => invitation.TokenHash,
                    normalizedHash))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TripAdmissionFenceWriteResult> PrepareFenceAsync(
        TripPlanId tripPlanId,
        TripInvitationId invitationId,
        string candidateUserId,
        string operationId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = NormalizeRequired(candidateUserId, nameof(candidateUserId));
        string normalizedOperationId = NormalizeRequired(operationId, nameof(operationId));
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        BsonDocument eligibilityExpression = new("$expr", new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$lt", new BsonArray
            {
                new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray
                {
                    "$members",
                    new BsonArray(),
                })),
                TripPlan.MaximumMembers,
            }),
            new BsonDocument("$eq", new BsonArray
            {
                new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
                {
                    { "input", new BsonDocument("$ifNull", new BsonArray { "$members", new BsonArray() }) },
                    { "as", "member" },
                    { "cond", new BsonDocument("$eq", new BsonArray { "$$member.userId", normalizedUserId }) },
                })),
                0,
            }),
            new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("$in", new BsonArray
                {
                    new BsonDocument("$type", "$memberAdmissionFence"),
                    new BsonArray { "missing", "null" },
                }),
                new BsonDocument("$eq", new BsonArray
                {
                    "$memberAdmissionFence.state",
                    TripMemberAdmissionFenceState.Cancelled.ToString(),
                }),
            }),
        }));
        FilterDefinition<TripPlanDocument> filter = filters.Eq(
                static plan => plan.Id,
                tripPlanId.Value)
            & filters.Eq(static plan => plan.DeletionState, TripDeletionState.None)
            & filters.Eq(static plan => plan.AdmissionClosureState, TripAdmissionClosureState.Open)
            & filters.In(static plan => plan.Status, new[]
            {
                TripPlanStatus.Draft,
                TripPlanStatus.OpenForVotes,
                TripPlanStatus.Decided,
            })
            & new BsonDocumentFilterDefinition<TripPlanDocument>(eligibilityExpression);
        BsonValue nextGeneration = new BsonDocument("$add", new BsonArray
        {
            new BsonDocument("$ifNull", new BsonArray { "$memberAdmissionGeneration", 0 }),
            1,
        });
        BsonValue leaseExpiry = new BsonDocument("$dateAdd", new BsonDocument
        {
            { "startDate", "$$NOW" },
            { "unit", "second" },
            { "amount", AdmissionLeaseSeconds },
        });
        PipelineUpdateDefinition<TripPlanDocument> update = new(new[]
        {
            new BsonDocument("$set", new BsonDocument
            {
                { "memberAdmissionGeneration", nextGeneration },
                {
                    "memberAdmissionFence",
                    new BsonDocument
                    {
                        { "invitationId", invitationId.Value },
                        { "operationId", normalizedOperationId },
                        { "candidateUserId", normalizedUserId },
                        { "generation", nextGeneration },
                        { "leaseExpiresAtUtc", leaseExpiry },
                        { "state", TripMemberAdmissionFenceState.Prepared.ToString() },
                    }
                },
                { "updatedAt", "$$NOW" },
            }),
        });
        TripPlanDocument? prepared = await this.plans.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (prepared?.MemberAdmissionFence is not null)
        {
            return new TripAdmissionFenceWriteResult(
                TripAdmissionWriteOutcome.Success,
                prepared.ToDomain().MemberAdmissionFence);
        }

        TripPlanDocument? current = await this.plans.Find(
                filters.Eq(static plan => plan.Id, tripPlanId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        TripMemberAdmissionFenceDocument? currentFenceDocument = current?.MemberAdmissionFence;
        if (currentFenceDocument is not null
            && string.Equals(currentFenceDocument.InvitationId, invitationId.Value, StringComparison.Ordinal)
            && string.Equals(currentFenceDocument.OperationId, normalizedOperationId, StringComparison.Ordinal)
            && string.Equals(currentFenceDocument.CandidateUserId, normalizedUserId, StringComparison.Ordinal))
        {
            TripMemberAdmissionFence currentFence = TripMemberAdmissionFence.Restore(
                invitationId,
                currentFenceDocument.OperationId,
                currentFenceDocument.CandidateUserId,
                currentFenceDocument.Generation,
                DateTime.SpecifyKind(currentFenceDocument.LeaseExpiresAtUtc, DateTimeKind.Utc),
                currentFenceDocument.State);
            return new TripAdmissionFenceWriteResult(
                TripAdmissionWriteOutcome.AlreadyCompleted,
                currentFence);
        }

        return new TripAdmissionFenceWriteResult(
            current is null
                ? TripAdmissionWriteOutcome.NotFound
                : current.Members.Count >= TripPlan.MaximumMembers
                    ? TripAdmissionWriteOutcome.MemberLimitReached
                    : TripAdmissionWriteOutcome.Conflict);
    }

    public async Task<TripAdmissionWriteOutcome> ReserveInvitationAsync(
        TripInvitation invitation,
        string candidateUserId,
        string operationId,
        string operationKeyHash,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        ArgumentNullException.ThrowIfNull(fence);
        string normalizedUserId = NormalizeRequired(candidateUserId, nameof(candidateUserId));
        string normalizedOperationId = NormalizeRequired(operationId, nameof(operationId));
        FilterDefinitionBuilder<TripInvitationDocument> filters = Builders<TripInvitationDocument>.Filter;
        FilterDefinition<TripInvitationDocument> serverTimeGuard =
            new BsonDocumentFilterDefinition<TripInvitationDocument>(new BsonDocument(
                "$expr",
                new BsonDocument("$and", new BsonArray
                {
                    new BsonDocument("$lt", new BsonArray { "$$NOW", "$expiresAtUtc" }),
                    new BsonDocument("$lt", new BsonArray
                    {
                        "$$NOW",
                        new BsonDateTime(fence.LeaseExpiresAtUtc),
                    }),
                })));
        TripInvitationDocument? reserved = await this.invitations.FindOneAndUpdateAsync(
            filters.Eq(static item => item.Id, invitation.Id.Value)
            & filters.Eq(static item => item.Status, TripInvitationStatus.Active)
            & filters.Eq(static item => item.Version, invitation.Version)
            & serverTimeGuard,
            Builders<TripInvitationDocument>.Update
                .Set(static item => item.Status, TripInvitationStatus.Accepting)
                .Set(static item => item.AcceptingUserId, normalizedUserId)
                .Set(static item => item.AcceptanceOperationId, normalizedOperationId)
                .Set(static item => item.AcceptanceOperationKeyHash, NormalizeRequired(operationKeyHash, nameof(operationKeyHash)))
                .Set(static item => item.AcceptanceGeneration, fence.Generation)
                .Set(static item => item.AcceptanceLeaseExpiresAtUtc, fence.LeaseExpiresAtUtc)
                .Inc(static item => item.Version, 1)
                .CurrentDate(static item => item.UpdatedAt)
                .Unset(static item => item.ActiveSlot)
                .Unset(static item => item.SealedToken)
                .Unset(static item => item.SealedTokenKeyVersion),
            new FindOneAndUpdateOptions<TripInvitationDocument, TripInvitationDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (reserved is not null)
        {
            return TripAdmissionWriteOutcome.Success;
        }

        TripInvitationDocument? current = await this.invitations.Find(
                filters.Eq(static item => item.Id, invitation.Id.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return IsSameAcceptance(current, normalizedUserId, normalizedOperationId, fence.Generation)
            ? TripAdmissionWriteOutcome.AlreadyCompleted
            : current is null
                ? TripAdmissionWriteOutcome.NotFound
                : TripAdmissionWriteOutcome.Conflict;
    }

    public Task<TripAdmissionWriteOutcome> ArmFenceAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        return this.UpdateFenceStateAsync(
            tripPlanId,
            fence,
            TripMemberAdmissionFenceState.Prepared,
            TripMemberAdmissionFenceState.Active,
            cancellationToken);
    }

    public async Task<TripAdmissionWriteOutcome> ApplyProvisionalMemberAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        TripDelegatedRole role,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fence);
        string memberId = TripMemberId.New().Value;
        FilterDefinition<TripPlanDocument> filter = BuildFenceFilter(
                tripPlanId,
                fence,
                TripMemberAdmissionFenceState.Active,
                true)
            & new BsonDocumentFilterDefinition<TripPlanDocument>(new BsonDocument(
                "$expr",
                new BsonDocument("$and", new BsonArray
                {
                    new BsonDocument("$lt", new BsonArray
                    {
                        new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray
                        {
                            "$members",
                            new BsonArray(),
                        })),
                        TripPlan.MaximumMembers,
                    }),
                    new BsonDocument("$eq", new BsonArray
                    {
                        new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", "$members" },
                            { "as", "member" },
                            { "cond", new BsonDocument("$eq", new BsonArray
                            {
                                "$$member.userId",
                                fence.CandidateUserId,
                            }) },
                        })),
                        0,
                    }),
                })));
        PipelineUpdateDefinition<TripPlanDocument> update = new(new[]
        {
            new BsonDocument("$set", new BsonDocument
            {
                {
                    "members",
                    new BsonDocument("$concatArrays", new BsonArray
                    {
                        "$members",
                        new BsonArray
                        {
                            new BsonDocument
                            {
                                { "memberId", memberId },
                                { "userId", fence.CandidateUserId },
                                { "delegatedRole", role.ToString() },
                                { "state", TripMembershipState.Provisional.ToString() },
                                { "joinedAtUtc", "$$NOW" },
                                { "memberDataEpoch", 1 },
                                { "admissionOperationId", fence.OperationId },
                            },
                        },
                    })
                },
                { "memberAdmissionFence.state", TripMemberAdmissionFenceState.Applied.ToString() },
                { "updatedAt", "$$NOW" },
            }),
        });
        UpdateResult result = await this.plans.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 1)
        {
            return TripAdmissionWriteOutcome.Success;
        }

        return await this.HasAdmittedMemberAsync(tripPlanId, fence, cancellationToken)
            ? TripAdmissionWriteOutcome.AlreadyCompleted
            : TripAdmissionWriteOutcome.Conflict;
    }

    public async Task<TripAdmissionWriteOutcome> MarkInvitationAcceptedAsync(
        TripInvitationId invitationId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripInvitationDocument> filters = Builders<TripInvitationDocument>.Filter;
        UpdateResult result = await this.invitations.UpdateOneAsync(
            filters.Eq(static item => item.Id, invitationId.Value)
            & filters.Eq(static item => item.Status, TripInvitationStatus.Accepting)
            & filters.Eq(static item => item.AcceptanceOperationId, fence.OperationId)
            & filters.Eq(static item => item.AcceptanceGeneration, fence.Generation),
            Builders<TripInvitationDocument>.Update
                .Set(static item => item.Status, TripInvitationStatus.Accepted)
                .Set(static item => item.UseCount, 1)
                .CurrentDate(static item => item.AcceptedAtUtc)
                .CurrentDate(static item => item.UpdatedAt)
                .Inc(static item => item.Version, 1),
            cancellationToken: cancellationToken);
        if (result.ModifiedCount == 1)
        {
            return TripAdmissionWriteOutcome.Success;
        }

        bool completed = await this.invitations.Find(
                filters.Eq(static item => item.Id, invitationId.Value)
                & filters.Eq(static item => item.Status, TripInvitationStatus.Accepted)
                & filters.Eq(static item => item.AcceptanceOperationId, fence.OperationId)
                & filters.Eq(static item => item.AcceptanceGeneration, fence.Generation))
            .AnyAsync(cancellationToken);
        return completed ? TripAdmissionWriteOutcome.AlreadyCompleted : TripAdmissionWriteOutcome.Conflict;
    }

    public async Task<TripAdmissionWriteOutcome> EstablishMemberAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> filter = BuildFenceFilter(
                tripPlanId,
                fence,
                TripMemberAdmissionFenceState.Applied,
                false)
            & filters.Eq(static plan => plan.AdmissionClosureState, TripAdmissionClosureState.Open)
            & filters.Eq(static plan => plan.DeletionState, TripDeletionState.None)
            & filters.ElemMatch(
                static plan => plan.Members,
                member => member.UserId == fence.CandidateUserId
                    && member.State == TripMembershipState.Provisional
                    && member.AdmissionOperationId == fence.OperationId);
        UpdateOptions options = new()
        {
            ArrayFilters = new[]
            {
                new BsonDocumentArrayFilterDefinition<TripMemberDocument>(new BsonDocument("$and", new BsonArray
                {
                    new BsonDocument("member.userId", fence.CandidateUserId),
                    new BsonDocument("member.state", TripMembershipState.Provisional.ToString()),
                    new BsonDocument("member.admissionOperationId", fence.OperationId),
                })),
            },
        };
        UpdateResult result = await this.plans.UpdateOneAsync(
            filter,
            Builders<TripPlanDocument>.Update
                .Set("members.$[member].state", TripMembershipState.Active.ToString())
                .Unset("members.$[member].admissionOperationId")
                .Unset(static plan => plan.MemberAdmissionFence)
                .Inc(static plan => plan.Version, 1)
                .CurrentDate(static plan => plan.UpdatedAt),
            options,
            cancellationToken);
        if (result.ModifiedCount == 1)
        {
            return TripAdmissionWriteOutcome.Success;
        }

        bool active = await this.plans.Find(
                filters.Eq(static plan => plan.Id, tripPlanId.Value)
                & filters.ElemMatch(
                    static plan => plan.Members,
                    member => member.UserId == fence.CandidateUserId
                        && member.State == TripMembershipState.Active))
            .AnyAsync(cancellationToken);
        return active ? TripAdmissionWriteOutcome.AlreadyCompleted : TripAdmissionWriteOutcome.Conflict;
    }

    public Task<TripAdmissionWriteOutcome> CancelFenceAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        return this.CancelFenceCoreAsync(tripPlanId, fence, false, cancellationToken);
    }

    public Task<TripAdmissionWriteOutcome> CancelExpiredFenceAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        return this.CancelFenceCoreAsync(tripPlanId, fence, true, cancellationToken);
    }

    private async Task<TripAdmissionWriteOutcome> CancelFenceCoreAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        bool requireExpired,
        CancellationToken cancellationToken)
    {
        FilterDefinition<TripPlanDocument> filter = BuildFenceIdentityFilter(tripPlanId, fence);
        if (requireExpired)
        {
            filter = BuildExpiredFenceCancellationFilter(tripPlanId, fence);
        }

        UpdateResult result = await this.plans.UpdateOneAsync(
            filter,
            Builders<TripPlanDocument>.Update
                .PullFilter(
                    static plan => plan.Members,
                    member => member.AdmissionOperationId == fence.OperationId)
                .Unset(static plan => plan.MemberAdmissionFence)
                .CurrentDate(static plan => plan.UpdatedAt),
            cancellationToken: cancellationToken);
        if (result.ModifiedCount == 1)
        {
            return TripAdmissionWriteOutcome.Success;
        }

        return await this.HasAdmittedMemberAsync(tripPlanId, fence, cancellationToken)
            ? TripAdmissionWriteOutcome.AlreadyCompleted
            : TripAdmissionWriteOutcome.Conflict;
    }

    public async Task CancelInvitationAcceptanceAsync(
        TripInvitationId invitationId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripInvitationDocument> filters = Builders<TripInvitationDocument>.Filter;
        await this.invitations.UpdateOneAsync(
            filters.Eq(static item => item.Id, invitationId.Value)
            & filters.In(static item => item.Status, new[]
            {
                TripInvitationStatus.Accepting,
                TripInvitationStatus.Accepted,
                TripInvitationStatus.RevocationPending,
            })
            & filters.Eq(static item => item.AcceptanceOperationId, fence.OperationId)
            & filters.Eq(static item => item.AcceptanceGeneration, fence.Generation),
            Builders<TripInvitationDocument>.Update
                .Set(static item => item.Status, TripInvitationStatus.Revoked)
                .Set(static item => item.UseCount, 0)
                .CurrentDate(static item => item.RevokedAtUtc)
                .CurrentDate(static item => item.UpdatedAt)
                .Unset(static item => item.AcceptedAtUtc)
                .Unset(static item => item.AcceptingUserId)
                .Unset(static item => item.AcceptanceOperationId)
                .Unset(static item => item.AcceptanceOperationKeyHash)
                .Unset(static item => item.AcceptanceGeneration)
                .Unset(static item => item.AcceptanceLeaseExpiresAtUtc)
                .Inc(static item => item.Version, 1),
            cancellationToken: cancellationToken);
    }

    public async Task<TripAdmissionWriteOutcome> DeclineInvitationAsync(
        TripInvitation invitation,
        string candidateUserId,
        string operationKeyHash,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripInvitationDocument> filters = Builders<TripInvitationDocument>.Filter;
        FilterDefinition<TripInvitationDocument> serverTimeGuard =
            new BsonDocumentFilterDefinition<TripInvitationDocument>(new BsonDocument(
                "$expr",
                new BsonDocument("$lt", new BsonArray { "$$NOW", "$expiresAtUtc" })));
        UpdateResult result = await this.invitations.UpdateOneAsync(
            filters.Eq(static item => item.Id, invitation.Id.Value)
            & filters.Eq(static item => item.Status, TripInvitationStatus.Active)
            & filters.Eq(static item => item.Version, invitation.Version)
            & serverTimeGuard,
            Builders<TripInvitationDocument>.Update
                .Set(static item => item.Status, TripInvitationStatus.Declined)
                .Set(static item => item.AcceptanceOperationKeyHash, NormalizeRequired(operationKeyHash, nameof(operationKeyHash)))
                .CurrentDate(static item => item.DeclinedAtUtc)
                .CurrentDate(static item => item.UpdatedAt)
                .Unset(static item => item.ActiveSlot)
                .Unset(static item => item.SealedToken)
                .Unset(static item => item.SealedTokenKeyVersion)
                .Inc(static item => item.Version, 1),
            cancellationToken: cancellationToken);
        if (result.ModifiedCount == 1)
        {
            return TripAdmissionWriteOutcome.Success;
        }

        TripInvitationDocument? current = await this.invitations.Find(
                filters.Eq(static item => item.Id, invitation.Id.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return current?.Status == TripInvitationStatus.Declined
            && string.Equals(current.AcceptanceOperationKeyHash, operationKeyHash, StringComparison.Ordinal)
                ? TripAdmissionWriteOutcome.AlreadyCompleted
                : current is null
                    ? TripAdmissionWriteOutcome.NotFound
                    : TripAdmissionWriteOutcome.Conflict;
    }

    public async Task<IReadOnlyCollection<TripInvitation>> ListPendingAcceptancesAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        List<TripInvitationDocument> documents = await this.invitations.Find(
                Builders<TripInvitationDocument>.Filter.In(
                    static item => item.Status,
                    new[] { TripInvitationStatus.Accepting, TripInvitationStatus.Accepted }))
            .SortBy(static item => item.UpdatedAt)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<TripPlan>> ListPendingAdmissionFencesAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        List<TripPlanDocument> documents = await this.plans.Find(
                Builders<TripPlanDocument>.Filter.Type(
                    static plan => plan.MemberAdmissionFence,
                    BsonType.Document))
            .SortBy(static plan => plan.MemberAdmissionFence!.LeaseExpiresAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    private async Task<TripAdmissionWriteOutcome> UpdateFenceStateAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        TripMemberAdmissionFenceState expectedState,
        TripMemberAdmissionFenceState nextState,
        CancellationToken cancellationToken)
    {
        UpdateResult result = await this.plans.UpdateOneAsync(
            BuildFenceFilter(tripPlanId, fence, expectedState, true),
            Builders<TripPlanDocument>.Update
                .Set(static plan => plan.MemberAdmissionFence!.State, nextState)
                .CurrentDate(static plan => plan.UpdatedAt),
            cancellationToken: cancellationToken);
        if (result.ModifiedCount == 1)
        {
            return TripAdmissionWriteOutcome.Success;
        }

        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> completedFilter = BuildFenceIdentityFilter(tripPlanId, fence)
            & filters.In(static plan => plan.MemberAdmissionFence!.State, new[]
            {
                nextState,
                TripMemberAdmissionFenceState.Applied,
            });
        bool completed = await this.plans.Find(completedFilter)
            .AnyAsync(cancellationToken);
        return completed ? TripAdmissionWriteOutcome.AlreadyCompleted : TripAdmissionWriteOutcome.Conflict;
    }

    private async Task<bool> HasAdmittedMemberAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> provisional = filters.ElemMatch(
            static plan => plan.Members,
            member => member.UserId == fence.CandidateUserId
                && member.State == TripMembershipState.Provisional
                && member.AdmissionOperationId == fence.OperationId);
        FilterDefinition<TripPlanDocument> active = filters.ElemMatch(
            static plan => plan.Members,
            member => member.UserId == fence.CandidateUserId
                && member.State == TripMembershipState.Active);
        return await this.plans.Find(
                filters.Eq(static plan => plan.Id, tripPlanId.Value)
                & (provisional | active))
            .AnyAsync(cancellationToken);
    }

    private static FilterDefinition<TripPlanDocument> BuildFenceFilter(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        TripMemberAdmissionFenceState state,
        bool requireUnexpired)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> filter = BuildFenceIdentityFilter(tripPlanId, fence)
            & filters.Eq(static plan => plan.MemberAdmissionFence!.State, state)
            & filters.Eq(static plan => plan.AdmissionClosureState, TripAdmissionClosureState.Open)
            & filters.Eq(static plan => plan.DeletionState, TripDeletionState.None)
            & filters.In(static plan => plan.Status, new[]
            {
                TripPlanStatus.Draft,
                TripPlanStatus.OpenForVotes,
                TripPlanStatus.Decided,
            });
        if (!requireUnexpired)
        {
            return filter;
        }

        return filter & new BsonDocumentFilterDefinition<TripPlanDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$lt", new BsonArray
            {
                "$$NOW",
                "$memberAdmissionFence.leaseExpiresAtUtc",
            })));
    }

    private static FilterDefinition<TripPlanDocument> BuildFenceIdentityFilter(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return filters.Eq(static plan => plan.Id, tripPlanId.Value)
            & filters.Eq(static plan => plan.MemberAdmissionFence!.InvitationId, fence.InvitationId.Value)
            & filters.Eq(static plan => plan.MemberAdmissionFence!.OperationId, fence.OperationId)
            & filters.Eq(static plan => plan.MemberAdmissionFence!.CandidateUserId, fence.CandidateUserId)
            & filters.Eq(static plan => plan.MemberAdmissionFence!.Generation, fence.Generation);
    }

    internal static FilterDefinition<TripPlanDocument> BuildExpiredFenceCancellationFilter(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence)
    {
        return BuildFenceIdentityFilter(tripPlanId, fence)
            & new BsonDocumentFilterDefinition<TripPlanDocument>(new BsonDocument(
                "$expr",
                new BsonDocument("$gte", new BsonArray
                {
                    "$$NOW",
                    "$memberAdmissionFence.leaseExpiresAtUtc",
                })));
    }

    private static bool IsSameAcceptance(
        TripInvitationDocument? invitation,
        string candidateUserId,
        string operationId,
        long generation)
    {
        return invitation is not null
            && invitation.Status is TripInvitationStatus.Accepting or TripInvitationStatus.Accepted
            && string.Equals(invitation.AcceptingUserId, candidateUserId, StringComparison.Ordinal)
            && string.Equals(invitation.AcceptanceOperationId, operationId, StringComparison.Ordinal)
            && invitation.AcceptanceGeneration == generation;
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("A required trip admission value is missing.", parameterName);
    }
}
