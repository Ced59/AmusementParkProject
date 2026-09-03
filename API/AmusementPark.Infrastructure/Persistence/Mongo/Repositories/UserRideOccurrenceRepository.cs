using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceRepository : IRideOccurrenceRepository
{
    public const int MaximumBatchSize = 100;

    public const int MaximumListSize = 250;

    private const string CreationOperationKind = "creation";

    private const string ReorderOperationKind = "reorder";

    private const string PendingOperationState = "pending";

    private const string CompletedOperationState = "completed";

    private const string ConflictOperationState = "conflict";

    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;
    private readonly UserRideOccurrenceReorderRecovery reorderRecovery;
    private readonly UserRideOccurrenceOrderGuardValidator orderGuardValidator;
    private readonly UserRideOccurrenceCreationRecovery creationRecovery;
    private readonly UserRideOccurrenceDeleteOperationCoordinator deletionCoordinator;
    private readonly UserRideOccurrenceVersionFence versionFence;
    private readonly UserRideOccurrencePendingOperationRecovery pendingOperationRecovery;

    public UserRideOccurrenceRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        this.collection = database.GetCollection<UserRideOccurrenceDocument>(
            settings.UserRideOccurrencesCollectionName);
        this.operationCollection =
            database.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                settings.UserRideOccurrenceOperationsCollectionName);
        this.reorderRecovery = new UserRideOccurrenceReorderRecovery(this.collection);
        this.orderGuardValidator = new UserRideOccurrenceOrderGuardValidator(
            this.collection,
            this.operationCollection);
        this.creationRecovery = new UserRideOccurrenceCreationRecovery(this.collection);
        this.deletionCoordinator = new UserRideOccurrenceDeleteOperationCoordinator(
            this.collection,
            this.operationCollection);
        this.versionFence = new UserRideOccurrenceVersionFence(this.collection);
        this.pendingOperationRecovery = new UserRideOccurrencePendingOperationRecovery(
            this.collection,
            this.operationCollection,
            this.orderGuardValidator,
            this.creationRecovery,
            this.deletionCoordinator);
    }

    public async Task<IdempotentRideOccurrenceCreationResult?> ResolveExistingBatchCreationAsync(
        RideOccurrenceCreationRequest request,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ValidateCreationRequest(request);
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(request);
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.LoadCreationOperationAsync(
                request.UserId,
                operationKeyHash,
                cancellationToken);
        if (operation is null)
        {
            return null;
        }

        if (!UserRideOccurrenceOperationValidator.CreationMatches(
            operation,
            payloadHash,
            request.Items.Count))
        {
            return CreateConflictResult();
        }

        List<UserRideOccurrenceDocument> existing = await this.LoadCreationDocumentsAsync(
            request.UserId,
            operationKeyHash,
            cancellationToken);
        IdempotentRideOccurrenceCreationResult? resolution = ResolveAgainstOperation(
            operation,
            existing,
            payloadHash,
            request.Items.Count);
        if (resolution is not null)
        {
            if (resolution.Status == IdempotentRideOccurrenceCreationStatus.Replayed
                && !string.Equals(
                    operation.OperationState,
                    CompletedOperationState,
                    StringComparison.Ordinal))
            {
                await this.pendingOperationRecovery.SetStateAsync(
                    operation,
                    CompletedOperationState,
                    cancellationToken);
            }

            return resolution;
        }

        RideOccurrenceOrderGuardValidationStatus validation =
            await this.orderGuardValidator.EnsureAppendBaseValidatedAsync(
                operation,
                cancellationToken);
        if (validation == RideOccurrenceOrderGuardValidationStatus.Stale)
        {
            bool deleted = await this.pendingOperationRecovery.DeleteUnvalidatedCreationAsync(
                operation,
                cancellationToken);
            return deleted
                ? null
                : CreateCreationConcurrencyConflictResult();
        }

        if (validation != RideOccurrenceOrderGuardValidationStatus.Validated)
        {
            return CreateCreationConcurrencyConflictResult();
        }

        IdempotentRideOccurrenceCreationResult recovered =
            await this.creationRecovery.RecoverAsync(
                operation,
                existing,
                payloadHash,
                request.Items.Count,
                cancellationToken);
        if (recovered.Status == IdempotentRideOccurrenceCreationStatus.Replayed)
        {
            await this.pendingOperationRecovery.SetStateAsync(
                operation,
                CompletedOperationState,
                cancellationToken);
        }

        return recovered;
    }

    public async Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAsync(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        long? expectedLastSortPosition,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ValidateCreationRequest(request);
        BatchScope scope = ValidateBatch(occurrences);
        ValidateCreationRequestMatchesBatch(request, occurrences, scope);
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(request);
        (UserRideOccurrenceCreationOperationDocument Operation, bool IsNew)? reservation =
            await this.EnsureCreationOperationAsync(
                occurrences,
                scope.UserId,
                scope.VisitId,
                expectedLastSortPosition,
                operationKeyHash,
                payloadHash,
                cancellationToken);
        if (!reservation.HasValue)
        {
            return CreateCreationConcurrencyConflictResult();
        }

        UserRideOccurrenceCreationOperationDocument operation = reservation.Value.Operation;
        bool isNewOperation = reservation.Value.IsNew;
        if (!UserRideOccurrenceOperationValidator.CreationMatches(
            operation,
            payloadHash,
            occurrences.Count))
        {
            return CreateConflictResult();
        }

        RideOccurrenceOrderGuardValidationStatus validation =
            await this.orderGuardValidator.EnsureAppendBaseValidatedAsync(
                operation,
                cancellationToken);
        if (validation == RideOccurrenceOrderGuardValidationStatus.Stale)
        {
            await this.pendingOperationRecovery.DeleteUnvalidatedCreationAsync(
                operation,
                cancellationToken);
            return CreateCreationConcurrencyConflictResult();
        }

        if (validation != RideOccurrenceOrderGuardValidationStatus.Validated)
        {
            return CreateCreationConcurrencyConflictResult();
        }

        IReadOnlyList<UserRideOccurrenceCreationAllocationDocument> allocations =
            operation.Items
                .OrderBy(static item => item.Index)
                .ToArray();
        List<UserRideOccurrenceDocument> documents = occurrences
            .Select((occurrence, index) => CreateCreationDocument(
                occurrence,
                allocations[index],
                operationKeyHash,
                payloadHash,
                occurrences.Count))
            .ToList();

        try
        {
            await this.collection.InsertManyAsync(
                documents,
                new InsertManyOptions { IsOrdered = false },
                cancellationToken);
            IReadOnlyCollection<RideOccurrence> created = documents
                .Select(static document => document.CreationSnapshotToDomain())
                .ToArray();
            await this.pendingOperationRecovery.SetStateAsync(
                operation,
                CompletedOperationState,
                cancellationToken);
            return new IdempotentRideOccurrenceCreationResult(
                isNewOperation
                    ? IdempotentRideOccurrenceCreationStatus.Created
                    : IdempotentRideOccurrenceCreationStatus.Replayed,
                created);
        }
        catch (MongoBulkWriteException<UserRideOccurrenceDocument> exception)
            when (ContainsOnlyDuplicateKeyErrors(exception))
        {
            List<UserRideOccurrenceDocument> existing = await this.LoadCreationDocumentsAsync(
                scope.UserId,
                operationKeyHash,
                cancellationToken);
            IdempotentRideOccurrenceCreationResult? resolution =
                ResolveAgainstOperation(
                    operation,
                    existing,
                    payloadHash,
                    occurrences.Count);
            if (resolution is null)
            {
                throw;
            }

            if (resolution.Status == IdempotentRideOccurrenceCreationStatus.Replayed)
            {
                await this.pendingOperationRecovery.SetStateAsync(
                    operation,
                    CompletedOperationState,
                    cancellationToken);
            }

            return resolution;
        }
    }

    public async Task<RideOccurrence?> GetOwnedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        UserRideOccurrenceDocument? document = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildOwnedOccurrenceFilter(
                occurrenceId.Value,
                visitId.Value,
                userId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<RideOccurrencePage> ListOwnedByVisitAsync(
        RideOccurrenceListCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.Limit is < 1 or > MaximumListSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(criteria),
                $"The ride occurrence list size must be between 1 and {MaximumListSize}.");
        }

        List<UserRideOccurrenceDocument> documents = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildListFilter(criteria))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildVisitOrderSort())
            .Limit(criteria.Limit + 1)
            .ToListAsync(cancellationToken);
        bool hasNextPage = documents.Count > criteria.Limit;
        List<RideOccurrence> occurrences = documents
            .Take(criteria.Limit)
            .Select(static document => document.ToDomain())
            .ToList();
        RideOccurrenceListCursor? nextCursor = hasNextPage && occurrences.Count > 0
            ? new RideOccurrenceListCursor(
                occurrences[^1].SortPosition,
                occurrences[^1].CreatedAtUtc,
                occurrences[^1].Id)
            : null;
        return new RideOccurrencePage(occurrences, nextCursor);
    }

    public async Task<long?> GetLastSortPositionAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        UserRideOccurrenceDocument? document = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildActiveVisitFilter(
                visitId.Value,
                userId))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildReverseVisitOrderSort())
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.SortPosition;
    }

    public async Task<bool> TryUpdateOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (expectedVersion == long.MaxValue || occurrence.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The persisted ride occurrence must be exactly one version ahead of the expected version.",
                nameof(occurrence));
        }

        UserRideOccurrenceDocument document = occurrence.ToDocument();
        UpdateResult result = await this.collection.UpdateOneAsync(
            UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                document.Id,
                document.VisitId,
                document.UserId,
                expectedVersion),
            BuildDomainUpdate(document),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> TryConfirmOwnedVersionAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return await this.versionFence.TryConfirmOwnedAsync(
            occurrenceId,
            visitId,
            userId,
            expectedVersion,
            cancellationToken);
    }

    public async Task<bool> TryDeleteOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (!occurrence.IsDeleted
            || expectedVersion == long.MaxValue
            || occurrence.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The deleted ride occurrence must be exactly one version ahead of the expected version.",
                nameof(occurrence));
        }

        UserRideOccurrenceDocument document = occurrence.ToDocument();
        return await this.deletionCoordinator.TryReserveAndApplyAsync(
            document,
            expectedVersion,
            recoveryCancellationToken => this.pendingOperationRecovery.TryCompleteVisitAsync(
                document.UserId,
                VisitId.Parse(document.VisitId),
                this.ResumeReservedReorderAsync,
                recoveryCancellationToken),
            cancellationToken);
    }

    public async Task<IdempotentRideOccurrenceReorderResult?> ResolveExistingReorderAsync(
        RideOccurrenceReorderRequest request,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ValidateReorderRequest(request);
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.LoadCreationOperationAsync(
                request.UserId,
                operationKeyHash,
                cancellationToken);
        if (operation is null)
        {
            return null;
        }

        return await this.ApplyReorderOperationAsync(
            operation,
            request,
            operationKeyHash,
            true,
            cancellationToken);
    }

    public async Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ValidateReorderRequest(request);
        ValidateReorderChanges(request, changes, guards, resultOccurrence);
        if (operationAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The operation timestamp must be UTC.", nameof(operationAtUtc));
        }
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string payloadHash =
            UserRideOccurrenceCreationFingerprint.HashReorderPayload(request);
        UserRideOccurrenceCreationOperationDocument requested = CreateReorderOperation(
            request,
            changes,
            guards,
            resultOccurrence,
            wasNormalized,
            operationAtUtc,
            operationKeyHash,
            payloadHash);
        UserRideOccurrenceCreationOperationDocument operation;
        bool wasExisting;
        try
        {
            await this.operationCollection.InsertOneAsync(
                requested,
                cancellationToken: cancellationToken);
            operation = requested;
            wasExisting = false;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            UserRideOccurrenceCreationOperationDocument? existing =
                await this.LoadCreationOperationAsync(
                    request.UserId,
                    operationKeyHash,
                    cancellationToken);
            if (existing is null)
            {
                bool recovered = await this.pendingOperationRecovery.TryCompleteVisitAsync(
                    request.UserId,
                    request.VisitId,
                    this.ResumeReservedReorderAsync,
                    cancellationToken);
                if (!recovered)
                {
                    return CreateReorderConflictResult(wasNormalized);
                }

                try
                {
                    await this.operationCollection.InsertOneAsync(
                        requested,
                        cancellationToken: cancellationToken);
                    operation = requested;
                    wasExisting = false;
                }
                catch (MongoWriteException retryException)
                    when (retryException.WriteError?.Category
                        == ServerErrorCategory.DuplicateKey)
                {
                    existing = await this.LoadCreationOperationAsync(
                        request.UserId,
                        operationKeyHash,
                        cancellationToken);
                    if (existing is null)
                    {
                        return CreateReorderConflictResult(wasNormalized);
                    }

                    operation = existing;
                    wasExisting = true;
                }
            }
            else
            {
                operation = existing;
                wasExisting = true;
            }
        }

        return await this.ApplyReorderOperationAsync(
            operation,
            request,
            operationKeyHash,
            wasExisting,
            cancellationToken);
    }

    internal static IdempotentRideOccurrenceCreationResult? ResolveIdempotentBatchCreation(
        IReadOnlyCollection<UserRideOccurrenceDocument> existing,
        string payloadHash,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(existing);
        if (expectedCount is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedCount));
        }

        if (existing.Count == 0)
        {
            return null;
        }

        bool metadataMatches = existing.All(document =>
            string.Equals(document.CreationPayloadHash, payloadHash, StringComparison.Ordinal)
            && document.CreationOperationCount == expectedCount
            && document.CreationOperationIndex is >= 0
            && document.CreationOperationIndex < expectedCount);
        int distinctIndexes = existing
            .Where(static document => document.CreationOperationIndex.HasValue)
            .Select(static document => document.CreationOperationIndex!.Value)
            .Distinct()
            .Count();
        if (!metadataMatches
            || existing.Count > expectedCount
            || distinctIndexes != existing.Count)
        {
            return new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Conflict,
                Array.Empty<RideOccurrence>());
        }

        if (existing.Count < expectedCount)
        {
            return null;
        }

        IReadOnlyCollection<RideOccurrence> replayed = existing
            .OrderBy(static document => document.CreationOperationIndex)
            .Select(static document => document.CreationSnapshotToDomain())
            .ToArray();
        return new IdempotentRideOccurrenceCreationResult(
            IdempotentRideOccurrenceCreationStatus.Replayed,
            replayed);
    }

    internal static IdempotentRideOccurrenceCreationResult? ResolveAgainstOperation(
        UserRideOccurrenceCreationOperationDocument operation,
        IReadOnlyCollection<UserRideOccurrenceDocument> existing,
        string payloadHash,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(existing);
        if (!UserRideOccurrenceOperationValidator.CreationMatches(
            operation,
            payloadHash,
            expectedCount))
        {
            return CreateConflictResult();
        }

        IReadOnlyDictionary<int, UserRideOccurrenceCreationAllocationDocument> allocations =
            operation.Items.ToDictionary(static item => item.Index);
        bool allocationsMatch = existing.All(document =>
            document.CreationOperationIndex.HasValue
            && document.CreationSnapshot is not null
            && allocations.TryGetValue(
                document.CreationOperationIndex.Value,
                out UserRideOccurrenceCreationAllocationDocument? allocation)
            && string.Equals(document.Id, allocation.OccurrenceId, StringComparison.Ordinal)
            && document.CreationSnapshot.SortPosition == allocation.SortPosition
            && document.CreationSnapshot.CreatedAtUtc == allocation.CreatedAtUtc
            && document.CreationSnapshot.UpdatedAtUtc == allocation.UpdatedAtUtc);
        if (!allocationsMatch)
        {
            return CreateConflictResult();
        }

        return ResolveIdempotentBatchCreation(existing, payloadHash, expectedCount);
    }

    internal static UpdateDefinition<UserRideOccurrenceDocument> BuildDomainUpdate(
        UserRideOccurrenceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        List<UpdateDefinition<UserRideOccurrenceDocument>> definitions =
            new List<UpdateDefinition<UserRideOccurrenceDocument>>
            {
                updates.Set(static item => item.SortPosition, document.SortPosition),
                updates.Set(static item => item.Moment, document.Moment),
                updates.Set(static item => item.Status, document.Status),
                updates.Set(
                    static item => item.HistoricalConsistency,
                    document.HistoricalConsistency),
                updates.Set(static item => item.Version, document.Version),
                updates.Set(static item => item.UpdatedAt, document.UpdatedAt),
            };
        AddOptionalUpdate(definitions, updates, "historicalTarget", document.HistoricalTarget);
        AddOptionalUpdate(definitions, updates, "privateNote", document.PrivateNote);
        AddOptionalUpdate(definitions, updates, "deletedAtUtc", document.DeletedAtUtc);
        return updates.Combine(definitions);
    }

    private async Task<List<UserRideOccurrenceDocument>> LoadCreationDocumentsAsync(
        string userId,
        string operationKeyHash,
        CancellationToken cancellationToken)
    {
        return await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildCreationOperationFilter(
                userId,
                operationKeyHash))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildCreationOperationSort())
            .ToListAsync(cancellationToken);
    }

    private async Task<UserRideOccurrenceCreationOperationDocument?> LoadCreationOperationAsync(
        string userId,
        string operationKeyHash,
        CancellationToken cancellationToken)
    {
        return await this.operationCollection
            .Find(UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                userId,
                operationKeyHash))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<IdempotentRideOccurrenceReorderResult> ResumeReservedReorderAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        RideOccurrenceReorderRequest request,
        CancellationToken cancellationToken)
    {
        return this.ApplyReorderOperationAsync(
            operation,
            request,
            operation.OperationKeyHash,
            true,
            cancellationToken);
    }

    private async Task<IdempotentRideOccurrenceReorderResult> ApplyReorderOperationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        RideOccurrenceReorderRequest request,
        string operationKeyHash,
        bool wasExisting,
        CancellationToken cancellationToken)
    {
        string payloadHash =
            UserRideOccurrenceCreationFingerprint.HashReorderPayload(request);
        if (!UserRideOccurrenceOperationValidator.ReorderMatches(
            operation,
            request,
            payloadHash))
        {
            return CreateReorderIdempotencyConflictResult(operation.WasNormalized);
        }

        if (string.Equals(
            operation.OperationState,
            ConflictOperationState,
            StringComparison.Ordinal))
        {
            return CreateReorderConflictResult(operation.WasNormalized);
        }

        if (!string.Equals(
            operation.OperationState,
            CompletedOperationState,
            StringComparison.Ordinal))
        {
            if (!operation.OrderGuardsValidated)
            {
                RideOccurrenceOrderGuardValidationStatus guardStatus =
                    await this.orderGuardValidator.EnsureValidatedAsync(
                    operation,
                    request,
                    cancellationToken);
                if (guardStatus == RideOccurrenceOrderGuardValidationStatus.Stale)
                {
                    bool conflictWasReserved = await this.pendingOperationRecovery
                        .TrySetUnvalidatedReorderConflictAsync(
                            operation,
                            cancellationToken);
                    if (conflictWasReserved)
                    {
                        return CreateReorderConflictResult(operation.WasNormalized);
                    }

                    guardStatus = await this.orderGuardValidator.EnsureValidatedAsync(
                        operation,
                        request,
                        cancellationToken);
                }

                if (guardStatus != RideOccurrenceOrderGuardValidationStatus.Validated)
                {
                    return CreateReorderConflictResult(operation.WasNormalized);
                }

                if (string.Equals(
                    operation.OperationState,
                    CompletedOperationState,
                    StringComparison.Ordinal))
                {
                    return CreateReorderReplayResult(operation);
                }

                if (!string.Equals(
                    operation.OperationState,
                    PendingOperationState,
                    StringComparison.Ordinal))
                {
                    return CreateReorderConflictResult(operation.WasNormalized);
                }
            }

            bool movedOccurrenceHasAllocation = operation.ReorderItems!.Any(item =>
                string.Equals(
                    item.OccurrenceId,
                    request.OccurrenceId.Value,
                    StringComparison.Ordinal));
            if (!movedOccurrenceHasAllocation
                && !await this.versionFence.TryApplyUnchangedReorderAsync(
                    request,
                    operationKeyHash,
                    cancellationToken))
            {
                await this.pendingOperationRecovery.SetStateAsync(
                    operation,
                    ConflictOperationState,
                    cancellationToken);
                return CreateReorderConflictResult(operation.WasNormalized);
            }

            List<UserRideOccurrenceReorderAllocationDocument> appliedAllocations =
                new List<UserRideOccurrenceReorderAllocationDocument>();
            foreach (UserRideOccurrenceReorderAllocationDocument allocation in
                operation.ReorderItems!.OrderBy(static item => item.Index))
            {
                UpdateResult update = await this.collection.UpdateOneAsync(
                    UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                        allocation.OccurrenceId,
                        request.VisitId.Value,
                        request.UserId,
                        allocation.ExpectedVersion),
                    BuildReorderUpdate(allocation, operationKeyHash),
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken);
                if (update.MatchedCount == 1)
                {
                    appliedAllocations.Add(allocation);
                    continue;
                }

                UserRideOccurrenceDocument? current = await this.collection
                    .Find(UserRideOccurrenceMongoDefinitions.BuildOwnedOccurrenceFilter(
                        allocation.OccurrenceId,
                        request.VisitId.Value,
                        request.UserId))
                    .FirstOrDefaultAsync(cancellationToken);
                if (current is not null
                    && UserRideOccurrenceReorderRecovery.AllocationWasApplied(
                        current,
                        allocation,
                        operationKeyHash))
                {
                    appliedAllocations.Add(allocation);
                    continue;
                }

                bool rolledBack = await this.reorderRecovery.TryRollbackAsync(
                    request,
                    appliedAllocations,
                    operationKeyHash,
                    cancellationToken);
                if (rolledBack)
                {
                    await this.pendingOperationRecovery.SetStateAsync(
                        operation,
                        ConflictOperationState,
                        cancellationToken);
                }

                return CreateReorderConflictResult(operation.WasNormalized);
            }

            await this.pendingOperationRecovery.SetStateAsync(
                operation,
                CompletedOperationState,
                cancellationToken);
            operation.OperationState = CompletedOperationState;
        }

        return wasExisting
            ? CreateReorderReplayResult(operation)
            : new IdempotentRideOccurrenceReorderResult(
                IdempotentRideOccurrenceReorderStatus.Applied,
                operation.ReorderResultSnapshot!.SnapshotToDomain(
                    operation.MovedOccurrenceId!,
                    operation.UserId),
                operation.WasNormalized);
    }

    private async Task<(UserRideOccurrenceCreationOperationDocument Operation, bool IsNew)?>
        EnsureCreationOperationAsync(
            IReadOnlyList<RideOccurrence> occurrences,
            string userId,
            VisitId visitId,
            long? expectedLastSortPosition,
            string operationKeyHash,
            string payloadHash,
            CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument requested =
            CreateCreationOperation(
                occurrences,
                userId,
                expectedLastSortPosition,
                operationKeyHash,
                payloadHash);
        try
        {
            await this.operationCollection.InsertOneAsync(
                requested,
                cancellationToken: cancellationToken);
            return (requested, true);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            UserRideOccurrenceCreationOperationDocument? existing =
                await this.LoadCreationOperationAsync(
                    userId,
                    operationKeyHash,
                    cancellationToken);
            if (existing is not null)
            {
                return (existing, false);
            }

            bool recovered = await this.pendingOperationRecovery.TryCompleteVisitAsync(
                userId,
                visitId,
                this.ResumeReservedReorderAsync,
                cancellationToken);
            if (!recovered)
            {
                return null;
            }

            try
            {
                await this.operationCollection.InsertOneAsync(
                    requested,
                    cancellationToken: cancellationToken);
                return (requested, true);
            }
            catch (MongoWriteException retryException)
                when (retryException.WriteError?.Category
                    == ServerErrorCategory.DuplicateKey)
            {
                existing = await this.LoadCreationOperationAsync(
                    userId,
                    operationKeyHash,
                    cancellationToken);
                return existing is null ? null : (existing, false);
            }
        }
    }

    internal static UserRideOccurrenceDocument CreateCreationDocument(
        RideOccurrence occurrence,
        UserRideOccurrenceCreationAllocationDocument allocation,
        string operationKeyHash,
        string payloadHash,
        int operationCount)
    {
        RideOccurrence reservedOccurrence = allocation.CreationSnapshot.SnapshotToDomain(
            allocation.OccurrenceId,
            occurrence.UserId);
        UserRideOccurrenceDocument document = reservedOccurrence.ToDocument();
        document.CreationOperationKeyHash = operationKeyHash;
        document.CreationPayloadHash = payloadHash;
        document.CreationOperationIndex = allocation.Index;
        document.CreationOperationCount = operationCount;
        document.CreationSnapshot = allocation.CreationSnapshot;
        return document;
    }

    internal static UserRideOccurrenceDocument CreateDocumentFromCreationAllocation(
        UserRideOccurrenceCreationOperationDocument operation,
        UserRideOccurrenceCreationAllocationDocument allocation,
        string payloadHash,
        int operationCount)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(allocation);
        RideOccurrence occurrence = allocation.CreationSnapshot.SnapshotToDomain(
            allocation.OccurrenceId,
            operation.UserId);
        return CreateCreationDocument(
            occurrence,
            allocation,
            operation.OperationKeyHash,
            payloadHash,
            operationCount);
    }

    private static UserRideOccurrenceCreationOperationDocument CreateCreationOperation(
        IReadOnlyList<RideOccurrence> occurrences,
        string userId,
        long? expectedLastSortPosition,
        string operationKeyHash,
        string payloadHash)
    {
        DateTime createdAtUtc = occurrences.Min(static occurrence => occurrence.CreatedAtUtc);
        return new UserRideOccurrenceCreationOperationDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            OperationKeyHash = operationKeyHash,
            PayloadHash = payloadHash,
            OperationKind = CreationOperationKind,
            VisitId = occurrences[0].VisitId.Value,
            OperationState = PendingOperationState,
            AppendBaseWasEmpty = !expectedLastSortPosition.HasValue,
            AppendBaseSortPosition = expectedLastSortPosition,
            Items = occurrences
                .Select((occurrence, index) =>
                {
                    UserRideOccurrenceDocument document = occurrence.ToDocument();
                    DateTime createdAtUtc = ToMongoPrecision(occurrence.CreatedAtUtc);
                    DateTime updatedAtUtc = ToMongoPrecision(occurrence.UpdatedAtUtc);
                    document.CreatedAt = createdAtUtc;
                    document.UpdatedAt = updatedAtUtc;
                    return new UserRideOccurrenceCreationAllocationDocument
                    {
                        Index = index,
                        OccurrenceId = occurrence.Id.Value,
                        SortPosition = occurrence.SortPosition,
                        CreatedAtUtc = createdAtUtc,
                        UpdatedAtUtc = updatedAtUtc,
                        CreationSnapshot = document.CreateCreationSnapshot(),
                    };
                })
                .ToList(),
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
        };
    }

    private static UserRideOccurrenceCreationOperationDocument CreateReorderOperation(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string operationKeyHash,
        string payloadHash)
    {
        UserRideOccurrenceDocument resultDocument = resultOccurrence.ToDocument();
        return new UserRideOccurrenceCreationOperationDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = request.UserId,
            OperationKeyHash = operationKeyHash,
            PayloadHash = payloadHash,
            OperationKind = ReorderOperationKind,
            VisitId = request.VisitId.Value,
            OperationState = PendingOperationState,
            MovedOccurrenceId = request.OccurrenceId.Value,
            ReorderExpectedVersion = request.ExpectedVersion,
            ReorderAnchorOccurrenceId = request.AnchorOccurrenceId?.Value,
            ReorderPlacement = request.Placement,
            WasNormalized = wasNormalized,
            ReorderItems = changes
                .Select((change, index) =>
                {
                    UserRideOccurrenceDocument document = change.Occurrence.ToDocument();
                    return new UserRideOccurrenceReorderAllocationDocument
                    {
                        Index = index,
                        OccurrenceId = change.Occurrence.Id.Value,
                        ExpectedVersion = change.ExpectedVersion,
                        PreviousSortPosition = change.PreviousSortPosition,
                        ResultSortPosition = document.SortPosition,
                        ResultVersion = document.Version,
                        ResultUpdatedAtUtc = document.UpdatedAt,
                    };
                })
                .ToList(),
            OrderGuards = guards
                .Select(static guard => new UserRideOccurrenceOrderGuardDocument
                {
                    OccurrenceId = guard.OccurrenceId.Value,
                    SortPosition = guard.SortPosition,
                })
                .ToList(),
            ReorderResultSnapshot = resultDocument.CreateCreationSnapshot(),
            CreatedAt = operationAtUtc,
            UpdatedAt = operationAtUtc,
        };
    }

    private static UpdateDefinition<UserRideOccurrenceDocument> BuildReorderUpdate(
        UserRideOccurrenceReorderAllocationDocument allocation,
        string operationKeyHash)
    {
        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        return updates.Combine(
            updates.Set(
                static document => document.SortPosition,
                allocation.ResultSortPosition),
            updates.Set(static document => document.Version, allocation.ResultVersion),
            updates.Set(
                static document => document.UpdatedAt,
                allocation.ResultUpdatedAtUtc),
            updates.Set(
                static document => document.LastReorderOperationKeyHash,
                operationKeyHash));
    }

    private static void ValidateReorderRequest(RideOccurrenceReorderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.VisitId.Value;
        _ = request.OccurrenceId.Value;
        _ = NormalizeRequired(request.UserId, nameof(request.UserId));
        if (request.ExpectedVersion < 1 || !Enum.IsDefined(request.Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(request));
        }
    }

    private static void ValidateReorderChanges(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(guards);
        ArgumentNullException.ThrowIfNull(resultOccurrence);
        if (changes.Count > RideOccurrenceOrderPlanner.MaximumReorderSize
            || guards.Count is < 1 or > RideOccurrenceOrderPlanner.MaximumReorderSize
            || resultOccurrence.Id != request.OccurrenceId
            || resultOccurrence.VisitId != request.VisitId
            || !string.Equals(resultOccurrence.UserId, request.UserId, StringComparison.Ordinal)
            || changes.Any(change =>
                change.ExpectedVersion < 1
                || change.Occurrence.VisitId != request.VisitId
                || !string.Equals(change.Occurrence.UserId, request.UserId, StringComparison.Ordinal)
                || change.PreviousSortPosition == change.Occurrence.SortPosition
                || change.Occurrence.Version != change.ExpectedVersion + 1)
            || guards.Select(static guard => guard.OccurrenceId).Distinct().Count()
                != guards.Count
            || !guards.Any(guard => guard.OccurrenceId == request.OccurrenceId))
        {
            throw new ArgumentException("The reorder plan is invalid.", nameof(changes));
        }
    }

    private static void ValidateCreationRequest(RideOccurrenceCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.VisitId.Value;
        _ = NormalizeRequired(request.UserId, nameof(request.UserId));
        if (request.Items.Count is < 1 or > MaximumBatchSize
            || request.Items.Any(item =>
                item is null
                || string.IsNullOrWhiteSpace(item.ParkItemId)
                || item.Moment is null
                || !Enum.IsDefined(item.Status)
                || !Enum.IsDefined(item.Source)))
        {
            throw new ArgumentException("The ride occurrence creation request is invalid.", nameof(request));
        }
    }

    private static void ValidateCreationRequestMatchesBatch(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        BatchScope scope)
    {
        if (request.VisitId != scope.VisitId
            || !string.Equals(request.UserId, scope.UserId, StringComparison.Ordinal)
            || request.Items.Count != occurrences.Count)
        {
            throw new ArgumentException(
                "The creation request does not match the occurrence batch.",
                nameof(request));
        }

        for (int index = 0; index < occurrences.Count; index++)
        {
            RideOccurrenceCreationRequestItem item = request.Items[index];
            RideOccurrence occurrence = occurrences[index];
            if (!string.Equals(item.ParkItemId, occurrence.ParkItemId, StringComparison.Ordinal)
                || item.Moment.LocalTime != occurrence.Moment.LocalTime
                || item.Moment.IsApproximate != occurrence.Moment.IsApproximate
                || item.Status != occurrence.Status
                || item.Source != occurrence.Source
                || !string.Equals(item.PrivateNote, occurrence.PrivateNote, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The creation request does not match the occurrence batch.",
                    nameof(request));
            }
        }
    }

    private static IdempotentRideOccurrenceReorderResult CreateReorderConflictResult(
        bool wasNormalized)
    {
        return new IdempotentRideOccurrenceReorderResult(
            IdempotentRideOccurrenceReorderStatus.Conflict,
            null,
            wasNormalized);
    }

    private static IdempotentRideOccurrenceReorderResult CreateReorderReplayResult(
        UserRideOccurrenceCreationOperationDocument operation)
    {
        return new IdempotentRideOccurrenceReorderResult(
            IdempotentRideOccurrenceReorderStatus.Replayed,
            operation.ReorderResultSnapshot!.SnapshotToDomain(
                operation.MovedOccurrenceId!,
                operation.UserId),
            operation.WasNormalized);
    }

    private static IdempotentRideOccurrenceReorderResult
        CreateReorderIdempotencyConflictResult(bool wasNormalized)
    {
        return new IdempotentRideOccurrenceReorderResult(
            IdempotentRideOccurrenceReorderStatus.IdempotencyConflict,
            null,
            wasNormalized);
    }

    private static IdempotentRideOccurrenceCreationResult CreateConflictResult()
    {
        return new IdempotentRideOccurrenceCreationResult(
            IdempotentRideOccurrenceCreationStatus.Conflict,
            Array.Empty<RideOccurrence>());
    }

    private static IdempotentRideOccurrenceCreationResult
        CreateCreationConcurrencyConflictResult()
    {
        return new IdempotentRideOccurrenceCreationResult(
            IdempotentRideOccurrenceCreationStatus.ConcurrencyConflict,
            Array.Empty<RideOccurrence>());
    }

    private static BatchScope ValidateBatch(IReadOnlyList<RideOccurrence> occurrences)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        if (occurrences.Count is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurrences),
                $"A ride occurrence batch must contain between 1 and {MaximumBatchSize} items.");
        }

        RideOccurrence first = occurrences[0]
            ?? throw new ArgumentException("A ride occurrence batch cannot contain null items.", nameof(occurrences));
        if (occurrences.Any(static occurrence => occurrence is null))
        {
            throw new ArgumentException("A ride occurrence batch cannot contain null items.", nameof(occurrences));
        }

        bool sameScope = occurrences.All(occurrence =>
            occurrence.VisitId == first.VisitId
            && string.Equals(occurrence.UserId, first.UserId, StringComparison.Ordinal)
            && string.Equals(occurrence.ParkId, first.ParkId, StringComparison.Ordinal)
            && !occurrence.IsDeleted
            && occurrence.Version == 1
            && occurrence.CreatedAtUtc == occurrence.UpdatedAtUtc);
        bool distinctIds = occurrences
            .Select(static occurrence => occurrence.Id)
            .Distinct()
            .Count() == occurrences.Count;
        bool distinctPositions = occurrences
            .Select(static occurrence => occurrence.SortPosition)
            .Distinct()
            .Count() == occurrences.Count;
        if (!sameScope || !distinctIds || !distinctPositions)
        {
            throw new ArgumentException(
                "All ride occurrences must be active, unique and belong to the same visit owner and park.",
                nameof(occurrences));
        }

        return new BatchScope(first.UserId, first.VisitId);
    }

    private static bool ContainsOnlyDuplicateKeyErrors(
        MongoBulkWriteException<UserRideOccurrenceDocument> exception)
    {
        return exception.WriteConcernError is null
            && exception.WriteErrors.Count > 0
            && exception.WriteErrors.All(
                static error => error.Category == ServerErrorCategory.DuplicateKey);
    }

    private static void AddOptionalUpdate<TValue>(
        ICollection<UpdateDefinition<UserRideOccurrenceDocument>> definitions,
        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates,
        string fieldName,
        TValue? value)
    {
        definitions.Add(value is null
            ? updates.Unset(fieldName)
            : updates.Set(fieldName, value));
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return normalizedValue;
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private sealed record BatchScope(string UserId, VisitId VisitId);
}
