using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class UserRideOccurrenceDeleteOperationCoordinator
{
    private const string DeleteOperationKind = "delete";
    private const string PendingOperationState = "pending";
    private const string CompletedOperationState = "completed";
    private const string ConflictOperationState = "conflict";

    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;

    public UserRideOccurrenceDeleteOperationCoordinator(
        IMongoCollection<UserRideOccurrenceDocument> collection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operationCollection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(operationCollection);
        this.collection = collection;
        this.operationCollection = operationCollection;
    }

    public async Task<bool> TryReserveAndApplyAsync(
        UserRideOccurrenceDocument deletedOccurrence,
        long expectedVersion,
        Func<CancellationToken, Task<bool>> recoverPendingOperationAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deletedOccurrence);
        ArgumentNullException.ThrowIfNull(recoverPendingOperationAsync);
        if (expectedVersion < 1
            || expectedVersion == long.MaxValue
            || deletedOccurrence.Version != expectedVersion + 1
            || !deletedOccurrence.DeletedAtUtc.HasValue)
        {
            throw new ArgumentException(
                "The deleted occurrence must be exactly one version ahead of the active occurrence.",
                nameof(deletedOccurrence));
        }

        UserRideOccurrenceCreationOperationDocument operation =
            CreateDeleteOperation(deletedOccurrence, expectedVersion);
        if (!await this.TryInsertOperationAsync(operation, cancellationToken))
        {
            bool recovered = await recoverPendingOperationAsync(cancellationToken);
            if (!recovered
                || !await this.TryInsertOperationAsync(operation, cancellationToken))
            {
                return false;
            }
        }

        return await this.TryCompleteAsync(operation, cancellationToken);
    }

    public async Task<bool> TryCompleteAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                operation.OperationKind,
                DeleteOperationKind,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(operation.VisitId)
            || string.IsNullOrWhiteSpace(operation.DeleteOccurrenceId)
            || operation.DeleteExpectedVersion is null or < 1
            || operation.DeleteExpectedVersion == long.MaxValue
            || !operation.DeleteAtUtc.HasValue)
        {
            _ = await this.SetStateAsync(
                operation,
                ConflictOperationState,
                cancellationToken);
            return false;
        }

        if (string.Equals(
            operation.OperationState,
            CompletedOperationState,
            StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.Equals(
            operation.OperationState,
            PendingOperationState,
            StringComparison.Ordinal))
        {
            return false;
        }

        long expectedVersion = operation.DeleteExpectedVersion.Value;
        DateTime deleteAtUtc = operation.DeleteAtUtc.Value;
        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        UpdateDefinition<UserRideOccurrenceDocument> update = updates.Combine(
            updates.Set(static document => document.DeletedAtUtc, deleteAtUtc),
            updates.Set(static document => document.Version, expectedVersion + 1),
            updates.Set(static document => document.UpdatedAt, deleteAtUtc),
            updates.Set(
                static document => document.LastDeleteOperationKeyHash,
                operation.OperationKeyHash),
            updates.Set(
                static document => document.ContentMutationFenceToken,
                operation.ContentMutationFenceToken));
        UpdateResult result = await this.collection.UpdateOneAsync(
            UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                    operation.DeleteOccurrenceId,
                    operation.VisitId,
                    operation.UserId,
                    expectedVersion),
                operation.ContentMutationFenceToken),
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);

        bool wasApplied = result.MatchedCount == 1;
        if (!wasApplied)
        {
            UserRideOccurrenceDocument? current = await this.collection
                .Find(UserRideOccurrenceMongoDefinitions.WithContentFence(
                    UserRideOccurrenceMongoDefinitions.BuildOwnedAnyStateFilter(
                        operation.DeleteOccurrenceId,
                        operation.VisitId,
                        operation.UserId),
                    operation.ContentMutationFenceToken))
                .FirstOrDefaultAsync(cancellationToken);
            wasApplied = current is not null
                && current.Version == expectedVersion + 1
                && current.DeletedAtUtc == deleteAtUtc
                && string.Equals(
                    current.LastDeleteOperationKeyHash,
                    operation.OperationKeyHash,
                    StringComparison.Ordinal);
        }

        bool statePersisted = await this.SetStateAsync(
            operation,
            wasApplied ? CompletedOperationState : ConflictOperationState,
            cancellationToken);
        return wasApplied && statePersisted;
    }

    private async Task<bool> TryInsertOperationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await this.operationCollection.InsertOneAsync(
                operation,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    private async Task<bool> SetStateAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        string state,
        CancellationToken cancellationToken)
    {
        UpdateDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> updates =
            Builders<UserRideOccurrenceCreationOperationDocument>.Update;
        List<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>> definitions =
            new List<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>
            {
                updates.Set(static document => document.OperationState, state),
                updates.Set(static document => document.UpdatedAt, operation.UpdatedAt),
            };
        if (string.Equals(state, ConflictOperationState, StringComparison.Ordinal))
        {
            definitions.Add(updates.Unset(
                static document => document.PendingAuditEvents));
        }

        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
            updates.Combine(definitions);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            UserRideOccurrenceCreationOperationMongoDefinitions.WithContentFence(
                UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                    operation.UserId,
                    operation.OperationKeyHash),
                operation.ContentMutationFenceToken),
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount == 1)
        {
            operation.OperationState = state;
            if (string.Equals(state, ConflictOperationState, StringComparison.Ordinal))
            {
                operation.PendingAuditEvents = null;
            }

            return true;
        }

        return false;
    }

    private static UserRideOccurrenceCreationOperationDocument CreateDeleteOperation(
        UserRideOccurrenceDocument deletedOccurrence,
        long expectedVersion)
    {
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            $"delete:{Guid.NewGuid():N}");
        DateTime deleteAtUtc = deletedOccurrence.DeletedAtUtc!.Value;
        return new UserRideOccurrenceCreationOperationDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = deletedOccurrence.UserId,
            OperationKeyHash = operationKeyHash,
            PayloadHash = operationKeyHash,
            OperationKind = DeleteOperationKind,
            VisitId = deletedOccurrence.VisitId,
            ContentMutationFenceToken = deletedOccurrence.ContentMutationFenceToken,
            OperationState = PendingOperationState,
            DeleteOccurrenceId = deletedOccurrence.Id,
            DeleteExpectedVersion = expectedVersion,
            DeleteAtUtc = deleteAtUtc,
            PendingAuditEvents = deletedOccurrence.PendingAuditEvents,
            CreatedAt = deleteAtUtc,
            UpdatedAt = deleteAtUtc,
        };
    }
}
