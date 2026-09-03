using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class UserRideOccurrencePendingOperationRecovery
{
    private const string CreationOperationKind = "creation";
    private const string CreationKeyReservationOperationKind =
        "creation-key-reservation";
    private const string DeleteOperationKind = "delete";
    private const string ReorderOperationKind = "reorder";
    private const string PendingOperationState = "pending";
    private const string ReservedOperationState = "reserved";
    private const string CompletedOperationState = "completed";
    private const string ConflictOperationState = "conflict";

    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;
    private readonly UserRideOccurrenceOrderGuardValidator orderGuardValidator;
    private readonly UserRideOccurrenceCreationRecovery creationRecovery;
    private readonly UserRideOccurrenceDeleteOperationCoordinator deletionCoordinator;

    public UserRideOccurrencePendingOperationRecovery(
        IMongoCollection<UserRideOccurrenceDocument> collection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operationCollection,
        UserRideOccurrenceOrderGuardValidator orderGuardValidator,
        UserRideOccurrenceCreationRecovery creationRecovery,
        UserRideOccurrenceDeleteOperationCoordinator deletionCoordinator)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(operationCollection);
        ArgumentNullException.ThrowIfNull(orderGuardValidator);
        ArgumentNullException.ThrowIfNull(creationRecovery);
        ArgumentNullException.ThrowIfNull(deletionCoordinator);
        this.collection = collection;
        this.operationCollection = operationCollection;
        this.orderGuardValidator = orderGuardValidator;
        this.creationRecovery = creationRecovery;
        this.deletionCoordinator = deletionCoordinator;
    }

    public async Task<bool> TryCompleteVisitAsync(
        string userId,
        VisitId visitId,
        Func<UserRideOccurrenceCreationOperationDocument,
            RideOccurrenceReorderRequest,
            CancellationToken,
            Task<IdempotentRideOccurrenceReorderResult>> resumeReorderAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resumeReorderAsync);
        UserRideOccurrenceCreationOperationDocument? pending =
            await this.LoadPendingVisitOperationAsync(
                userId,
                visitId,
                cancellationToken);
        if (pending is null)
        {
            return true;
        }

        if (string.Equals(
            pending.OperationKind,
            CreationOperationKind,
            StringComparison.Ordinal))
        {
            return await this.TryCompleteCreationAsync(
                pending,
                cancellationToken);
        }

        if (string.Equals(
            pending.OperationKind,
            DeleteOperationKind,
            StringComparison.Ordinal))
        {
            _ = await this.deletionCoordinator.TryCompleteAsync(
                pending,
                cancellationToken);
            UserRideOccurrenceCreationOperationDocument? stillPendingDelete =
                await this.LoadPendingVisitOperationAsync(
                    userId,
                    visitId,
                    cancellationToken);
            return stillPendingDelete is null;
        }

        if (!TryBuildReservedReorderRequest(pending, out RideOccurrenceReorderRequest request))
        {
            return false;
        }

        _ = await resumeReorderAsync(pending, request, cancellationToken);
        UserRideOccurrenceCreationOperationDocument? stillPending =
            await this.LoadPendingVisitOperationAsync(
                userId,
                visitId,
                cancellationToken);
        return stillPending is null;
    }

    public async Task<bool> ReleaseUnvalidatedCreationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.OperationKind, CreationOperationKind)
            & filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Eq(static document => document.AppendBaseValidated, false);
        if (operation.CreationPreparation is not null)
        {
            UpdateDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> updates =
                Builders<UserRideOccurrenceCreationOperationDocument>.Update;
            UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
                updates.Combine(
                    updates.Set(
                        static document => document.OperationKind,
                        CreationKeyReservationOperationKind),
                    updates.Set(
                        static document => document.OperationState,
                        ReservedOperationState),
                    updates.Set(
                        static document => document.AppendBaseWasEmpty,
                        false),
                    updates.Unset(
                        static document => document.AppendBaseSortPosition),
                    updates.Set(
                        static document => document.AppendBaseValidated,
                        false),
                    updates.Set(
                        static document => document.Items,
                        new List<UserRideOccurrenceCreationAllocationDocument>()),
                    updates.Unset(
                        static document => document.PendingAuditEvents),
                    updates.Set(
                        static document => document.UpdatedAt,
                        operation.UpdatedAt));
            UpdateResult updateResult = await this.operationCollection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = false },
                cancellationToken);
            if (updateResult.MatchedCount == 1)
            {
                operation.OperationKind = CreationKeyReservationOperationKind;
                operation.OperationState = ReservedOperationState;
                operation.AppendBaseWasEmpty = false;
                operation.AppendBaseSortPosition = null;
                operation.AppendBaseValidated = false;
                operation.Items.Clear();
                operation.PendingAuditEvents = null;
                return true;
            }

            return false;
        }

        DeleteResult result = await this.operationCollection.DeleteOneAsync(
            filter,
            cancellationToken);
        return result.DeletedCount == 1;
    }

    public async Task<bool> SetStateAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        string state,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
            BuildStateUpdate(state, operation.UpdatedAt);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash),
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount == 1)
        {
            ApplyStateTransition(operation, state);
            return true;
        }

        return false;
    }

    public async Task<bool> TrySetUnvalidatedReorderConflictAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.OperationKind, ReorderOperationKind)
            & filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Eq(static document => document.OrderGuardsValidated, false);
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
            BuildStateUpdate(ConflictOperationState, operation.UpdatedAt);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount == 1)
        {
            ApplyStateTransition(operation, ConflictOperationState);
            return true;
        }

        return false;
    }

    public async Task<bool> TryBeginReorderCompensationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.OperationKind, ReorderOperationKind)
            & filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Ne(static document => document.ReorderCompensationStarted, true);
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
            Builders<UserRideOccurrenceCreationOperationDocument>.Update
                .Set(static document => document.ReorderCompensationStarted, true)
                .Set(static document => document.UpdatedAt, operation.UpdatedAt);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount == 1)
        {
            operation.ReorderCompensationStarted = true;
            return true;
        }

        return false;
    }

    public async Task<bool> TryCompleteReorderAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.OperationKind, ReorderOperationKind)
            & filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Ne(static document => document.ReorderCompensationStarted, true);
        return await this.TryTransitionReorderStateAsync(
            operation,
            filter,
            CompletedOperationState,
            cancellationToken);
    }

    public async Task<bool> TryFinishReorderCompensationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.OperationKind, ReorderOperationKind)
            & filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Eq(static document => document.ReorderCompensationStarted, true);
        return await this.TryTransitionReorderStateAsync(
            operation,
            filter,
            ConflictOperationState,
            cancellationToken);
    }

    private async Task<bool> TryTransitionReorderStateAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter,
        string targetState,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
            BuildStateUpdate(targetState, operation.UpdatedAt);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount == 1)
        {
            ApplyStateTransition(operation, targetState);
            return true;
        }

        return false;
    }

    private static UpdateDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildStateUpdate(string state, DateTime updatedAtUtc)
    {
        UpdateDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> updates =
            Builders<UserRideOccurrenceCreationOperationDocument>.Update;
        List<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>> definitions =
            new List<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>
            {
                updates.Set(static document => document.OperationState, state),
                updates.Set(static document => document.UpdatedAt, updatedAtUtc),
            };
        if (string.Equals(state, ConflictOperationState, StringComparison.Ordinal))
        {
            definitions.Add(updates.Unset(
                static document => document.PendingAuditEvents));
        }

        return updates.Combine(definitions);
    }

    private static void ApplyStateTransition(
        UserRideOccurrenceCreationOperationDocument operation,
        string state)
    {
        operation.OperationState = state;
        if (string.Equals(state, ConflictOperationState, StringComparison.Ordinal))
        {
            operation.PendingAuditEvents = null;
        }
    }

    private async Task<bool> TryCompleteCreationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        int expectedCount = operation.Items.Count;
        if (!UserRideOccurrenceOperationValidator.CreationMatches(
            operation,
            operation.PayloadHash,
            expectedCount))
        {
            return false;
        }

        List<UserRideOccurrenceDocument> existing = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildCreationOperationFilter(
                operation.UserId,
                operation.OperationKeyHash))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildCreationOperationSort())
            .ToListAsync(cancellationToken);
        IdempotentRideOccurrenceCreationResult? resolution =
            UserRideOccurrenceRepository.ResolveAgainstOperation(
                operation,
                existing,
                operation.PayloadHash,
                expectedCount);
        if (resolution?.Status == IdempotentRideOccurrenceCreationStatus.Conflict)
        {
            return false;
        }

        if (resolution is null)
        {
            RideOccurrenceOrderGuardValidationStatus validation =
                await this.orderGuardValidator.EnsureAppendBaseValidatedAsync(
                    operation,
                    cancellationToken);
            if (validation == RideOccurrenceOrderGuardValidationStatus.Stale)
            {
                return await this.ReleaseUnvalidatedCreationAsync(
                    operation,
                    cancellationToken);
            }

            if (validation != RideOccurrenceOrderGuardValidationStatus.Validated)
            {
                return false;
            }

            resolution = await this.creationRecovery.RecoverAsync(
                operation,
                existing,
                operation.PayloadHash,
                expectedCount,
                cancellationToken);
        }

        return resolution.Status == IdempotentRideOccurrenceCreationStatus.Replayed
            && await this.SetStateAsync(
                operation,
                CompletedOperationState,
                cancellationToken);
    }

    private async Task<UserRideOccurrenceCreationOperationDocument?>
        LoadPendingVisitOperationAsync(
            string userId,
            VisitId visitId,
            CancellationToken cancellationToken)
    {
        return await this.operationCollection
            .Find(UserRideOccurrenceCreationOperationMongoDefinitions.BuildPendingVisitFilter(
                userId,
                visitId.Value))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool TryBuildReservedReorderRequest(
        UserRideOccurrenceCreationOperationDocument operation,
        out RideOccurrenceReorderRequest request)
    {
        request = null!;
        if (!string.Equals(
                operation.OperationKind,
                ReorderOperationKind,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(operation.VisitId)
            || string.IsNullOrWhiteSpace(operation.MovedOccurrenceId)
            || operation.ReorderExpectedVersion is null or < 1
            || !operation.ReorderPlacement.HasValue)
        {
            return false;
        }

        try
        {
            RideOccurrenceId? anchorId = string.IsNullOrWhiteSpace(
                operation.ReorderAnchorOccurrenceId)
                ? null
                : RideOccurrenceId.Parse(operation.ReorderAnchorOccurrenceId);
            request = new RideOccurrenceReorderRequest(
                VisitId.Parse(operation.VisitId),
                operation.UserId,
                RideOccurrenceId.Parse(operation.MovedOccurrenceId),
                operation.ReorderExpectedVersion.Value,
                anchorId,
                operation.ReorderPlacement.Value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
