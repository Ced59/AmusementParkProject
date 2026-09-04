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

    public const int MaximumPendingMutationScanSize = 50;

    private const string CreationOperationKind = "creation";

    private const string CreationKeyReservationOperationKind =
        "creation-key-reservation";

    private const string ReorderOperationKind = "reorder";

    private const string DeleteOperationKind = "delete";

    private const string PendingOperationState = "pending";

    private const string CompletedOperationState = "completed";

    private const string ConflictOperationState = "conflict";

    private const string ReservedOperationState = "reserved";

    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;
    private readonly IMongoCollection<UserVisitDocument>? visitCollection;
    private readonly UserRideOccurrenceReorderRecovery reorderRecovery;
    private readonly UserRideOccurrenceOrderGuardValidator orderGuardValidator;
    private readonly UserRideOccurrenceCreationRecovery creationRecovery;
    private readonly UserRideOccurrenceProvisionalCreationReconciler
        provisionalCreationReconciler;
    private readonly UserRideOccurrenceDeleteOperationCoordinator deletionCoordinator;
    private readonly UserRideOccurrenceVersionFence versionFence;
    private readonly UserRideOccurrencePendingOperationRecovery pendingOperationRecovery;

    public UserRideOccurrenceRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(
            GetCollection<UserRideOccurrenceDocument>(
                database,
                settings,
                static value => value.UserRideOccurrencesCollectionName),
            GetCollection<UserRideOccurrenceCreationOperationDocument>(
                database,
                settings,
                static value => value.UserRideOccurrenceOperationsCollectionName),
            GetCollection<UserVisitDocument>(
                database,
                settings,
                static value => value.UserVisitsCollectionName))
    {
    }

    internal UserRideOccurrenceRepository(
        IMongoCollection<UserRideOccurrenceDocument> collection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operationCollection,
        IMongoCollection<UserVisitDocument>? visitCollection = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(operationCollection);
        this.collection = collection;
        this.operationCollection = operationCollection;
        this.visitCollection = visitCollection;
        this.reorderRecovery = new UserRideOccurrenceReorderRecovery(this.collection);
        this.orderGuardValidator = new UserRideOccurrenceOrderGuardValidator(
            this.collection,
            this.operationCollection);
        this.creationRecovery = new UserRideOccurrenceCreationRecovery(this.collection);
        this.provisionalCreationReconciler =
            new UserRideOccurrenceProvisionalCreationReconciler(
                this.collection,
                this.operationCollection);
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

    public async Task<PendingPassportMutationVisit?> GetPendingMutationAsync(
        string userId,
        VisitId visitId,
        CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.operationCollection
                .Find(UserRideOccurrenceCreationOperationMongoDefinitions
                    .BuildPendingVisitFilter(userId, visitId.Value))
                .FirstOrDefaultAsync(cancellationToken);
        return operation is null
            ? null
            : CreatePendingMutation(operation, visitId);
    }

    public async Task<PendingPassportMutationVisit?> GetPendingMutationFencedAsync(
        string userId,
        VisitId visitId,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.operationCollection
                .Find(UserRideOccurrenceCreationOperationMongoDefinitions
                    .BuildPendingVisitFilter(
                        userId,
                        visitId.Value,
                        ValidateContentFence(contentFenceToken, nameof(contentFenceToken))))
                .FirstOrDefaultAsync(cancellationToken);
        if (operation is not null)
        {
            return CreatePendingMutation(operation, visitId);
        }

        return null;
    }

    public async Task<IReadOnlyCollection<PendingPassportMutationVisit>>
        ListPendingAuditMutationVisitsAsync(
        int maximumVisitCount,
        CancellationToken cancellationToken)
    {
        if (maximumVisitCount is < 1 or > MaximumPendingMutationScanSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisitCount));
        }

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Exists(PassportAuditMongoDefinitions.PendingEventIdPath, true);
        List<UserRideOccurrenceCreationOperationDocument> operations =
            await this.operationCollection
                .Find(filter)
                .Limit(maximumVisitCount)
                .ToListAsync(cancellationToken);
        List<PendingPassportMutationVisit> candidates =
            new List<PendingPassportMutationVisit>(operations.Count);
        foreach (UserRideOccurrenceCreationOperationDocument operation in operations
            .OrderBy(static document => document.UpdatedAt)
            .ThenBy(static document => document.Id, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(operation.VisitId))
            {
                continue;
            }

            VisitId visitId;
            try
            {
                visitId = VisitId.Parse(operation.VisitId);
            }
            catch (ArgumentException)
            {
                continue;
            }

            candidates.Add(CreatePendingMutation(operation, visitId));
        }

        return candidates;
    }

    public Task<int> ReconcileProvisionalCreationAllocationsAsync(
        int maximumDocumentCount,
        CancellationToken cancellationToken)
    {
        return this.provisionalCreationReconciler.ReconcileBatchAsync(
            maximumDocumentCount,
            cancellationToken);
    }

    public async Task<bool> TryCompletePendingMutationAsync(
        PendingPassportMutationVisit mutation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        if (mutation.ContentFenceToken.HasValue
            && await this.LoadCreationOperationAsync(
                mutation.UserId,
                mutation.VisitId,
                mutation.OperationKeyHash,
                mutation.ContentFenceToken,
                cancellationToken) is null)
        {
            return false;
        }

        return await this.pendingOperationRecovery.TryCompleteOperationAsync(
            mutation.UserId,
            mutation.VisitId,
            mutation.OperationKeyHash,
            mutation.ContentFenceToken,
            this.ResumeReservedReorderAsync,
            cancellationToken);
    }

    public async Task<bool> TryRejectPendingMutationAsync(
        PendingPassportMutationVisit mutation,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        UserRideOccurrenceCreationOperationDocument? operation = null;
        if (mutation.ContentFenceToken.HasValue)
        {
            operation = await this.LoadCreationOperationAsync(
                mutation.UserId,
                mutation.VisitId,
                mutation.OperationKeyHash,
                mutation.ContentFenceToken,
                cancellationToken);
            if (operation is null)
            {
                return false;
            }
        }

        if (mutation.Kind == PendingPassportMutationKind.Creation
            && operation is not null)
        {
            await this.creationRecovery.RemoveStaleAllocationsAsync(
                operation,
                cancellationToken);
        }

        return await this.pendingOperationRecovery.TrySetPendingConflictAsync(
            mutation.UserId,
            mutation.VisitId,
            mutation.OperationKeyHash,
            mutation.ContentFenceToken,
            rejectedAtUtc,
            cancellationToken);
    }

    public async Task<RideOccurrenceCreationKeyReservationResult>
        ResolveBatchCreationKeyReservationAsync(
            RideOccurrenceCreationRequest request,
            string clientOperationId,
            CancellationToken cancellationToken)
    {
        ValidateCreationRequest(request);
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(request);
        UserRideOccurrenceCreationOperationDocument? existing =
            await this.LoadCreationOperationAsync(
                request.UserId,
                request.VisitId,
                operationKeyHash,
                request.ContentFenceToken,
                cancellationToken);
        return CreateCreationKeyReservationResult(
            existing,
            request,
            payloadHash,
            RideOccurrenceCreationKeyReservationStatus.Replayed);
    }

    public async Task<RideOccurrenceCreationKeyReservationResult>
        ReserveBatchCreationKeyAsync(
            RideOccurrenceCreationRequest request,
            RideOccurrenceCreationPreparation preparation,
            string clientOperationId,
            DateTime reservedAtUtc,
            CancellationToken cancellationToken)
    {
        ValidateCreationRequest(request);
        ValidateCreationPreparation(request, preparation);
        if (reservedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The creation key reservation timestamp must be UTC.",
                nameof(reservedAtUtc));
        }

        string normalizedOperationId = NormalizeRequired(
            clientOperationId,
            nameof(clientOperationId));
        string operationKeyHash =
            UserRideOccurrenceCreationFingerprint.HashOperationKey(normalizedOperationId);
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(request);
        UserRideOccurrenceCreationOperationDocument reservation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = request.UserId,
                OperationKeyHash = operationKeyHash,
                PayloadHash = payloadHash,
                OperationKind = CreationKeyReservationOperationKind,
                VisitId = request.VisitId.Value,
                ContentMutationFenceToken = request.ContentFenceToken,
                OperationState = ReservedOperationState,
                CreationPreparation = CreatePreparationDocument(preparation),
                CreatedAt = reservedAtUtc,
                UpdatedAt = reservedAtUtc,
            };
        try
        {
            await this.operationCollection.InsertOneAsync(
                reservation,
                cancellationToken: cancellationToken);
            return new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Reserved,
                preparation);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            UserRideOccurrenceCreationOperationDocument? existing =
                await this.LoadCreationOperationAsync(
                    request.UserId,
                    request.VisitId,
                    operationKeyHash,
                    request.ContentFenceToken,
                    cancellationToken);
            if (existing is null)
            {
                return new RideOccurrenceCreationKeyReservationResult(
                    RideOccurrenceCreationKeyReservationStatus.Conflict);
            }

            return CreateCreationKeyReservationResult(
                existing,
                request,
                payloadHash,
                RideOccurrenceCreationKeyReservationStatus.Replayed);
        }
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
                request.VisitId,
                operationKeyHash,
                request.ContentFenceToken,
                cancellationToken);
        if (operation is null)
        {
            return null;
        }

        if (string.Equals(
            operation.OperationKind,
            CreationKeyReservationOperationKind,
            StringComparison.Ordinal))
        {
            RideOccurrenceCreationKeyReservationResult reservation =
                CreateCreationKeyReservationResult(
                    operation,
                    request,
                    payloadHash,
                    RideOccurrenceCreationKeyReservationStatus.Replayed);
            return reservation.Status == RideOccurrenceCreationKeyReservationStatus.Replayed
                ? null
                : CreateConflictResult();
        }

        if (!UserRideOccurrenceOperationValidator.CreationMatches(
            operation,
            payloadHash,
            request.Items.Count))
        {
            return CreateConflictResult();
        }

        bool normalizationSignalPersisted =
            await this.EnsureCreationNormalizationSignalAsync(
                operation,
                request.VisitId,
                cancellationToken);
        if (!normalizationSignalPersisted)
        {
            return null;
        }

        List<UserRideOccurrenceDocument> existing = await this.LoadCreationDocumentsAsync(
            request.UserId,
            request.VisitId,
            operationKeyHash,
            request.ContentFenceToken,
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
                bool completed = await this.pendingOperationRecovery.SetStateAsync(
                    operation,
                    CompletedOperationState,
                    cancellationToken);
                if (!completed)
                {
                    return CreateCreationConcurrencyConflictResult();
                }
            }

            return await this.EnsureCurrentCreationFenceAsync(
                resolution,
                request,
                cancellationToken);
        }

        RideOccurrenceOrderGuardValidationStatus validation =
            await this.orderGuardValidator.EnsureAppendBaseValidatedAsync(
                operation,
                cancellationToken);
        if (validation == RideOccurrenceOrderGuardValidationStatus.Stale)
        {
            bool released = await this.pendingOperationRecovery.ReleaseUnvalidatedCreationAsync(
                operation,
                cancellationToken);
            return released
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
            bool completed = await this.pendingOperationRecovery.SetStateAsync(
                operation,
                CompletedOperationState,
                cancellationToken);
            if (!completed)
            {
                return CreateCreationConcurrencyConflictResult();
            }
        }

        return await this.EnsureCurrentCreationFenceAsync(
            recovered,
            request,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAsync(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        long? expectedLastSortPosition,
        bool wasOrderNormalized,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.CreateBatchIdempotentCoreAsync(
            request,
            occurrences,
            expectedLastSortPosition,
            wasOrderNormalized,
            clientOperationId,
            null,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAuditedAsync(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        long? expectedLastSortPosition,
        bool wasOrderNormalized,
        string clientOperationId,
        IReadOnlyCollection<PassportAuditEvent> pendingAuditEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvents);
        return this.CreateBatchIdempotentCoreAsync(
            request,
            occurrences,
            expectedLastSortPosition,
            wasOrderNormalized,
            clientOperationId,
            pendingAuditEvents,
            cancellationToken);
    }

    private async Task<IdempotentRideOccurrenceCreationResult>
        CreateBatchIdempotentCoreAsync(
            RideOccurrenceCreationRequest request,
            IReadOnlyList<RideOccurrence> occurrences,
            long? expectedLastSortPosition,
            bool wasOrderNormalized,
            string clientOperationId,
            IReadOnlyCollection<PassportAuditEvent>? pendingAuditEvents,
            CancellationToken cancellationToken)
    {
        ValidateCreationRequest(request);
        BatchScope scope = ValidateBatch(occurrences);
        ValidateCreationRequestMatchesBatch(request, occurrences, scope);
        ValidatePendingAuditEvents(occurrences, pendingAuditEvents);
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(request);
        (UserRideOccurrenceCreationOperationDocument Operation, bool IsNew)? reservation =
            await this.EnsureCreationOperationAsync(
                occurrences,
                scope.UserId,
                scope.VisitId,
                request.ContentFenceToken,
                expectedLastSortPosition,
                wasOrderNormalized,
                operationKeyHash,
                payloadHash,
                pendingAuditEvents,
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
            await this.pendingOperationRecovery.ReleaseUnvalidatedCreationAsync(
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
                occurrences.Count,
                request.ContentFenceToken))
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
            bool completed = await this.pendingOperationRecovery.SetStateAsync(
                operation,
                CompletedOperationState,
                cancellationToken);
            if (!completed)
            {
                return CreateCreationConcurrencyConflictResult();
            }

            IdempotentRideOccurrenceCreationResult result =
                new IdempotentRideOccurrenceCreationResult(
                    isNewOperation
                        ? IdempotentRideOccurrenceCreationStatus.Created
                        : IdempotentRideOccurrenceCreationStatus.Replayed,
                    created,
                    operation.WasNormalized);
            return await this.EnsureCurrentCreationFenceAsync(
                result,
                request,
                cancellationToken);
        }
        catch (MongoBulkWriteException<UserRideOccurrenceDocument> exception)
            when (ContainsOnlyDuplicateKeyErrors(exception))
        {
            List<UserRideOccurrenceDocument> existing = await this.LoadCreationDocumentsAsync(
                scope.UserId,
                scope.VisitId,
                operationKeyHash,
                request.ContentFenceToken,
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
                bool completed = await this.pendingOperationRecovery.SetStateAsync(
                    operation,
                    CompletedOperationState,
                    cancellationToken);
                if (!completed)
                {
                    return CreateCreationConcurrencyConflictResult();
                }
            }

            return await this.EnsureCurrentCreationFenceAsync(
                resolution,
                request,
                cancellationToken);
        }
    }

    public async Task<RideOccurrence?> GetOwnedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        CurrentContentFence currentFence = await this.LoadCurrentContentFenceAsync(
            visitId.Value,
            userId,
            cancellationToken);
        if (!currentFence.VisitExists)
        {
            return null;
        }

        UserRideOccurrenceDocument? document = await this.collection
            .Find(WithCurrentContentFence(
                UserRideOccurrenceMongoDefinitions.BuildOwnedOccurrenceFilter(
                    occurrenceId.Value,
                    visitId.Value,
                    userId),
                currentFence))
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

        CurrentContentFence currentFence = await this.LoadCurrentContentFenceAsync(
            criteria.VisitId.Value,
            criteria.UserId,
            cancellationToken);
        if (!currentFence.VisitExists)
        {
            return new RideOccurrencePage(
                Array.Empty<RideOccurrence>(),
                null);
        }

        List<UserRideOccurrenceDocument> documents = await this.collection
            .Find(WithCurrentContentFence(
                UserRideOccurrenceMongoDefinitions.BuildListFilter(criteria),
                currentFence))
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

    public async Task<RideOccurrence?> GetOwnedByIdAsync(
        RideOccurrenceId occurrenceId,
        string userId,
        CancellationToken cancellationToken)
    {
        UserRideOccurrenceDocument? document = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildOwnedOccurrenceByIdFilter(
                occurrenceId.Value,
                userId))
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        CurrentContentFence currentFence = await this.LoadCurrentContentFenceAsync(
            document.VisitId,
            userId,
            cancellationToken);
        return currentFence.VisitExists && currentFence.Matches(document.ContentMutationFenceToken)
            ? document.ToDomain()
            : null;
    }

    public async Task<RideOccurrenceAppendState> GetAppendStateAsync(
        VisitId visitId,
        string userId,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = NormalizeRequired(userId, nameof(userId));
        string relatedCreationOperationKeyHash =
            UserRideOccurrenceCreationFingerprint.HashOperationKey(
                NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        CurrentContentFence currentFence = await this.LoadCurrentContentFenceAsync(
            visitId.Value,
            normalizedUserId,
            cancellationToken);
        if (!currentFence.VisitExists)
        {
            return new RideOccurrenceAppendState(null, false);
        }

        UserRideOccurrenceDocument? lastOccurrence = await this.collection
            .Find(WithCurrentContentFence(
                UserRideOccurrenceMongoDefinitions.BuildActiveVisitFilter(
                    visitId.Value,
                    normalizedUserId),
                currentFence))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildReverseVisitOrderSort())
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        bool wasNormalized = await this.WasCreationOrderNormalizedAsync(
            normalizedUserId,
            visitId,
            relatedCreationOperationKeyHash,
            currentFence.IsEnforced ? currentFence.Token : null,
            cancellationToken);
        return new RideOccurrenceAppendState(
            lastOccurrence?.SortPosition,
            wasNormalized);
    }

    public Task<bool> TryUpdateOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.TryUpdateOwnedCoreAsync(
            occurrence,
            expectedVersion,
            null,
            null,
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedAuditedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvent);
        return this.TryUpdateOwnedCoreAsync(
            occurrence,
            expectedVersion,
            pendingAuditEvent,
            null,
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.TryUpdateOwnedCoreAsync(
            occurrence,
            expectedVersion,
            null,
            ValidateContentFence(contentFenceToken, nameof(contentFenceToken)),
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedAuditedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvent);
        return this.TryUpdateOwnedCoreAsync(
            occurrence,
            expectedVersion,
            pendingAuditEvent,
            ValidateContentFence(contentFenceToken, nameof(contentFenceToken)),
            cancellationToken);
    }

    private async Task<bool> TryUpdateOwnedCoreAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent? pendingAuditEvent,
        long? contentFenceToken,
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
        document.ContentMutationFenceToken = contentFenceToken;
        if (pendingAuditEvent is not null)
        {
            ValidatePendingAuditEvent(occurrence, pendingAuditEvent);
        }

        UpdateResult result = await this.collection.UpdateOneAsync(
            UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                    document.Id,
                    document.VisitId,
                    document.UserId,
                    expectedVersion),
                contentFenceToken),
            BuildDomainUpdate(document, pendingAuditEvent),
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
            null,
            cancellationToken);
    }

    public async Task<bool> TryConfirmOwnedVersionFencedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return await this.versionFence.TryConfirmOwnedAsync(
            occurrenceId,
            visitId,
            userId,
            expectedVersion,
            ValidateContentFence(contentFenceToken, nameof(contentFenceToken)),
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.TryDeleteOwnedCoreAsync(
            occurrence,
            expectedVersion,
            null,
            null,
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedAuditedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvent);
        return this.TryDeleteOwnedCoreAsync(
            occurrence,
            expectedVersion,
            pendingAuditEvent,
            null,
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.TryDeleteOwnedCoreAsync(
            occurrence,
            expectedVersion,
            null,
            ValidateContentFence(contentFenceToken, nameof(contentFenceToken)),
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedAuditedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvent);
        return this.TryDeleteOwnedCoreAsync(
            occurrence,
            expectedVersion,
            pendingAuditEvent,
            ValidateContentFence(contentFenceToken, nameof(contentFenceToken)),
            cancellationToken);
    }

    private async Task<bool> TryDeleteOwnedCoreAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent? pendingAuditEvent,
        long? contentFenceToken,
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
        document.ContentMutationFenceToken = contentFenceToken;
        if (pendingAuditEvent is not null)
        {
            ValidatePendingAuditEvent(occurrence, pendingAuditEvent);
            document.PendingAuditEvents = new List<PassportAuditEventDocument>
            {
                pendingAuditEvent.ToDocument(),
            };
        }

        return await this.deletionCoordinator.TryReserveAndApplyAsync(
            document,
            expectedVersion,
            recoveryCancellationToken => this.pendingOperationRecovery.TryCompleteVisitAsync(
                document.UserId,
                VisitId.Parse(document.VisitId),
                document.ContentMutationFenceToken,
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
                request.VisitId,
                operationKeyHash,
                request.ContentFenceToken,
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

    public Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        string? relatedCreationClientOperationId,
        CancellationToken cancellationToken)
    {
        return this.ReorderIdempotentCoreAsync(
            request,
            changes,
            guards,
            resultOccurrence,
            wasNormalized,
            operationAtUtc,
            clientOperationId,
            relatedCreationClientOperationId,
            null,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAuditedAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        string? relatedCreationClientOperationId,
        IReadOnlyCollection<PassportAuditEvent> pendingAuditEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvents);
        return this.ReorderIdempotentCoreAsync(
            request,
            changes,
            guards,
            resultOccurrence,
            wasNormalized,
            operationAtUtc,
            clientOperationId,
            relatedCreationClientOperationId,
            pendingAuditEvents,
            cancellationToken);
    }

    private async Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentCoreAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        string? relatedCreationClientOperationId,
        IReadOnlyCollection<PassportAuditEvent>? pendingAuditEvents,
        CancellationToken cancellationToken)
    {
        ValidateReorderRequest(request);
        ValidateReorderChanges(request, changes, guards, resultOccurrence);
        ValidatePendingAuditEvents(
            changes.Select(static change => change.Occurrence).ToArray(),
            pendingAuditEvents);
        if (operationAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The operation timestamp must be UTC.", nameof(operationAtUtc));
        }
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string? relatedCreationOperationKeyHash =
            string.IsNullOrWhiteSpace(relatedCreationClientOperationId)
                ? null
                : UserRideOccurrenceCreationFingerprint.HashOperationKey(
                    relatedCreationClientOperationId.Trim());
        string payloadHash =
            UserRideOccurrenceCreationFingerprint.HashReorderPayload(request);
        UserRideOccurrenceCreationOperationDocument requested = CreateReorderOperation(
            request,
            changes,
            guards,
            resultOccurrence,
            wasNormalized,
            relatedCreationOperationKeyHash,
            operationAtUtc,
            operationKeyHash,
            payloadHash,
            pendingAuditEvents);
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
                    request.VisitId,
                    operationKeyHash,
                    request.ContentFenceToken,
                    cancellationToken);
            if (existing is null)
            {
                bool recovered = await this.pendingOperationRecovery.TryCompleteVisitAsync(
                    request.UserId,
                    request.VisitId,
                    request.ContentFenceToken,
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
                        request.VisitId,
                        operationKeyHash,
                        request.ContentFenceToken,
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

        IdempotentRideOccurrenceCreationResult? result =
            ResolveIdempotentBatchCreation(existing, payloadHash, expectedCount);
        return result is null
            ? null
            : result with { WasNormalized = operation.WasNormalized };
    }

    internal static UpdateDefinition<UserRideOccurrenceDocument> BuildDomainUpdate(
        UserRideOccurrenceDocument document,
        PassportAuditEvent? pendingAuditEvent = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        List<UpdateDefinition<UserRideOccurrenceDocument>> definitions =
            new List<UpdateDefinition<UserRideOccurrenceDocument>>
            {
                updates.Set(static item => item.SchemaVersion, document.SchemaVersion),
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
        AddOptionalUpdate(definitions, updates, "assessment", document.Assessment);
        AddOptionalUpdate(definitions, updates, "deletedAtUtc", document.DeletedAtUtc);
        if (document.ContentMutationFenceToken.HasValue)
        {
            definitions.Add(updates.Set(
                static item => item.ContentMutationFenceToken,
                document.ContentMutationFenceToken));
        }
        if (pendingAuditEvent is not null)
        {
            definitions.Add(updates.Push(
                static item => item.PendingAuditEvents,
                pendingAuditEvent.ToDocument()));
        }

        return updates.Combine(definitions);
    }

    private async Task<List<UserRideOccurrenceDocument>> LoadCreationDocumentsAsync(
        string userId,
        VisitId visitId,
        string operationKeyHash,
        long? contentFenceToken,
        CancellationToken cancellationToken)
    {
        if (contentFenceToken.HasValue)
        {
            await this.PromoteCreationDocumentsFenceAsync(
                userId,
                visitId,
                operationKeyHash,
                contentFenceToken.Value,
                cancellationToken);
        }

        return await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildCreationOperationFilter(
                    userId,
                    operationKeyHash)
                & Builders<UserRideOccurrenceDocument>.Filter.Eq(
                    static document => document.VisitId,
                    visitId.Value),
                contentFenceToken))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildCreationOperationSort())
            .ToListAsync(cancellationToken);
    }

    private async Task<UserRideOccurrenceCreationOperationDocument?> LoadCreationOperationAsync(
        string userId,
        VisitId visitId,
        string operationKeyHash,
        long? contentFenceToken,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> visitOperation =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                userId,
                operationKeyHash)
            & filters.Eq(static document => document.VisitId, visitId.Value);
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.operationCollection
            .Find(UserRideOccurrenceCreationOperationMongoDefinitions.WithContentFence(
                visitOperation,
                contentFenceToken))
            .FirstOrDefaultAsync(cancellationToken);
        if (operation is not null || !contentFenceToken.HasValue)
        {
            return operation;
        }

        UserRideOccurrenceCreationOperationDocument? older =
            await this.operationCollection
                .Find(visitOperation)
                .FirstOrDefaultAsync(cancellationToken);
        if (older is null
            || older.ContentMutationFenceToken >= contentFenceToken.Value)
        {
            return null;
        }

        await this.PromoteCreationDocumentsFenceAsync(
            userId,
            visitId,
            operationKeyHash,
            contentFenceToken.Value,
            cancellationToken);
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> olderFenceFilter =
            older.ContentMutationFenceToken.HasValue
                ? filters.Eq(
                    static document => document.ContentMutationFenceToken,
                    older.ContentMutationFenceToken.Value)
                : filters.Or(
                    filters.Exists(
                        static document => document.ContentMutationFenceToken,
                        false),
                    filters.Eq(
                        static document => document.ContentMutationFenceToken,
                        null));
        _ = await this.operationCollection.UpdateOneAsync(
            visitOperation & olderFenceFilter,
            Builders<UserRideOccurrenceCreationOperationDocument>.Update.Set(
                static document => document.ContentMutationFenceToken,
                contentFenceToken.Value),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        await this.PromoteCreationDocumentsFenceAsync(
            userId,
            visitId,
            operationKeyHash,
            contentFenceToken.Value,
            cancellationToken);
        return await this.operationCollection
            .Find(UserRideOccurrenceCreationOperationMongoDefinitions.WithContentFence(
                visitOperation,
                contentFenceToken))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task PromoteCreationDocumentsFenceAsync(
        string userId,
        VisitId visitId,
        string operationKeyHash,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        FilterDefinition<UserRideOccurrenceDocument> olderFence = filters.Or(
            filters.Exists(
                static document => document.ContentMutationFenceToken,
                false),
            filters.Eq(
                static document => document.ContentMutationFenceToken,
                null),
            filters.Lt(
                static document => document.ContentMutationFenceToken,
                contentFenceToken));
        _ = await this.collection.UpdateManyAsync(
            UserRideOccurrenceMongoDefinitions.BuildCreationOperationFilter(
                userId,
                operationKeyHash)
            & filters.Eq(static document => document.VisitId, visitId.Value)
            & olderFence,
            Builders<UserRideOccurrenceDocument>.Update.Set(
                static document => document.ContentMutationFenceToken,
                contentFenceToken),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
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
            if (operation.ReorderCompensationStarted)
            {
                return await this.CompensateReorderAsync(
                    operation,
                    request,
                    operationKeyHash,
                    cancellationToken);
            }

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

            foreach (UserRideOccurrenceReorderAllocationDocument allocation in
                operation.ReorderItems!.OrderBy(static item => item.Index))
            {
                UpdateResult update = await this.collection.UpdateOneAsync(
                    UserRideOccurrenceMongoDefinitions.WithContentFence(
                        UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                            allocation.OccurrenceId,
                            request.VisitId.Value,
                            request.UserId,
                            allocation.ExpectedVersion),
                        request.ContentFenceToken),
                    BuildReorderUpdate(
                        allocation,
                        operationKeyHash,
                        request.ContentFenceToken),
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken);
                if (update.MatchedCount == 1)
                {
                    continue;
                }

                UserRideOccurrenceDocument? current = await this.collection
                    .Find(UserRideOccurrenceMongoDefinitions.WithContentFence(
                        UserRideOccurrenceMongoDefinitions.BuildOwnedOccurrenceFilter(
                            allocation.OccurrenceId,
                            request.VisitId.Value,
                            request.UserId),
                        request.ContentFenceToken))
                    .FirstOrDefaultAsync(cancellationToken);
                if (current is not null
                    && UserRideOccurrenceReorderRecovery.AllocationWasApplied(
                        current,
                        allocation,
                        operationKeyHash))
                {
                    continue;
                }

                return await this.ResolveReorderWriteFailureAsync(
                    operation,
                    request,
                    operationKeyHash,
                    cancellationToken);
            }

            bool completed = await this.pendingOperationRecovery.TryCompleteReorderAsync(
                operation,
                cancellationToken);
            if (!completed)
            {
                return await this.ResolveReorderCompletionRaceAsync(
                    operation,
                    request,
                    operationKeyHash,
                    cancellationToken);
            }
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

    private async Task<IdempotentRideOccurrenceReorderResult>
        ResolveReorderWriteFailureAsync(
            UserRideOccurrenceCreationOperationDocument operation,
            RideOccurrenceReorderRequest request,
            string operationKeyHash,
            CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument? durable =
            await this.LoadCreationOperationAsync(
                operation.UserId,
                request.VisitId,
                operationKeyHash,
                operation.ContentMutationFenceToken,
                cancellationToken);
        if (durable is null)
        {
            return CreateReorderConflictResult(operation.WasNormalized);
        }

        if (string.Equals(
            durable.OperationState,
            CompletedOperationState,
            StringComparison.Ordinal))
        {
            return CreateReorderReplayResult(durable);
        }

        if (!string.Equals(
            durable.OperationState,
            PendingOperationState,
            StringComparison.Ordinal))
        {
            return CreateReorderConflictResult(durable.WasNormalized);
        }

        operation.ReorderCompensationStarted = durable.ReorderCompensationStarted;
        if (!operation.ReorderCompensationStarted)
        {
            bool claimed = await this.pendingOperationRecovery
                .TryBeginReorderCompensationAsync(
                    operation,
                    cancellationToken);
            if (!claimed)
            {
                return await this.ResolveReorderCompletionRaceAsync(
                    operation,
                    request,
                    operationKeyHash,
                    cancellationToken);
            }
        }

        return await this.CompensateReorderAsync(
            operation,
            request,
            operationKeyHash,
            cancellationToken);
    }

    private async Task<IdempotentRideOccurrenceReorderResult>
        ResolveReorderCompletionRaceAsync(
            UserRideOccurrenceCreationOperationDocument operation,
            RideOccurrenceReorderRequest request,
            string operationKeyHash,
            CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument? durable =
            await this.LoadCreationOperationAsync(
                operation.UserId,
                request.VisitId,
                operationKeyHash,
                operation.ContentMutationFenceToken,
                cancellationToken);
        if (durable is not null
            && string.Equals(
                durable.OperationState,
                CompletedOperationState,
                StringComparison.Ordinal))
        {
            return CreateReorderReplayResult(durable);
        }

        if (durable is not null
            && string.Equals(
                durable.OperationState,
                PendingOperationState,
                StringComparison.Ordinal)
            && durable.ReorderCompensationStarted)
        {
            operation.ReorderCompensationStarted = true;
            return await this.CompensateReorderAsync(
                operation,
                request,
                operationKeyHash,
                cancellationToken);
        }

        return CreateReorderConflictResult(
            durable?.WasNormalized ?? operation.WasNormalized);
    }

    private async Task<IdempotentRideOccurrenceReorderResult> CompensateReorderAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        RideOccurrenceReorderRequest request,
        string operationKeyHash,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UserRideOccurrenceReorderAllocationDocument> allocations =
            operation.ReorderItems!
                .OrderBy(static item => item.Index)
                .ToArray();
        bool rolledBack = await this.reorderRecovery.TryRollbackAsync(
            request,
            allocations,
            operationKeyHash,
            cancellationToken);
        if (rolledBack)
        {
            _ = await this.pendingOperationRecovery.TryFinishReorderCompensationAsync(
                operation,
                cancellationToken);
        }

        return CreateReorderConflictResult(operation.WasNormalized);
    }

    private async Task<(UserRideOccurrenceCreationOperationDocument Operation, bool IsNew)?>
        EnsureCreationOperationAsync(
            IReadOnlyList<RideOccurrence> occurrences,
            string userId,
            VisitId visitId,
            long? contentFenceToken,
            long? expectedLastSortPosition,
            bool wasOrderNormalized,
            string operationKeyHash,
            string payloadHash,
            IReadOnlyCollection<PassportAuditEvent>? pendingAuditEvents,
            CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument requested =
            CreateCreationOperation(
                occurrences,
                userId,
                contentFenceToken,
                expectedLastSortPosition,
                wasOrderNormalized,
                operationKeyHash,
                payloadHash,
                pendingAuditEvents);
        try
        {
            await this.operationCollection.InsertOneAsync(
                requested,
                cancellationToken: cancellationToken);
            bool normalizationSignalPersisted =
                await this.EnsureCreationNormalizationSignalAsync(
                    requested,
                    visitId,
                    cancellationToken);
            if (!normalizationSignalPersisted)
            {
                return null;
            }

            return (requested, true);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            UserRideOccurrenceCreationOperationDocument? existing =
                await this.LoadCreationOperationAsync(
                    userId,
                    visitId,
                    operationKeyHash,
                    contentFenceToken,
                    cancellationToken);
            if (existing is not null)
            {
                if (CreationReservationMatchesBatch(existing, requested))
                {
                    return await this.ActivateCreationReservationAsync(
                        existing,
                        requested,
                        visitId,
                        cancellationToken);
                }

                bool normalizationSignalPersisted =
                    await this.EnsureCreationNormalizationSignalAsync(
                        existing,
                        visitId,
                        cancellationToken);
                if (!normalizationSignalPersisted)
                {
                    return null;
                }

                return (existing, false);
            }

            bool recovered = await this.pendingOperationRecovery.TryCompleteVisitAsync(
                userId,
                visitId,
                contentFenceToken,
                this.ResumeReservedReorderAsync,
                cancellationToken);
            if (!recovered)
            {
                return null;
            }

            requested.WasNormalized |= await this.WasCreationOrderNormalizedAsync(
                userId,
                visitId,
                operationKeyHash,
                requested.ContentMutationFenceToken,
                cancellationToken);

            try
            {
                await this.operationCollection.InsertOneAsync(
                    requested,
                    cancellationToken: cancellationToken);
                bool normalizationSignalPersisted =
                    await this.EnsureCreationNormalizationSignalAsync(
                        requested,
                        visitId,
                        cancellationToken);
                if (!normalizationSignalPersisted)
                {
                    return null;
                }

                return (requested, true);
            }
            catch (MongoWriteException retryException)
                when (retryException.WriteError?.Category
                    == ServerErrorCategory.DuplicateKey)
            {
                existing = await this.LoadCreationOperationAsync(
                    userId,
                    visitId,
                    operationKeyHash,
                    contentFenceToken,
                    cancellationToken);
                if (existing is null)
                {
                    return null;
                }

                bool normalizationSignalPersisted =
                    await this.EnsureCreationNormalizationSignalAsync(
                        existing,
                        visitId,
                        cancellationToken);
                return normalizationSignalPersisted
                    ? (existing, false)
                    : null;
            }
        }
    }

    private async Task<(UserRideOccurrenceCreationOperationDocument Operation, bool IsNew)?>
        ActivateCreationReservationAsync(
            UserRideOccurrenceCreationOperationDocument reservation,
            UserRideOccurrenceCreationOperationDocument requested,
            VisitId visitId,
            CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            requested.WasNormalized |= await this.WasCreationOrderNormalizedAsync(
                requested.UserId,
                visitId,
                requested.OperationKeyHash,
                requested.ContentMutationFenceToken,
                cancellationToken);
            try
            {
                bool activated = await this.TryActivateCreationReservationAsync(
                    reservation,
                    requested,
                    cancellationToken);
                if (activated)
                {
                    bool activationSignalPersisted =
                        await this.EnsureCreationNormalizationSignalAsync(
                            requested,
                            visitId,
                            cancellationToken);
                    return activationSignalPersisted
                        ? (requested, true)
                        : null;
                }
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category
                    == ServerErrorCategory.DuplicateKey)
            {
                bool recovered = await this.pendingOperationRecovery.TryCompleteVisitAsync(
                    requested.UserId,
                    visitId,
                    requested.ContentMutationFenceToken,
                    this.ResumeReservedReorderAsync,
                    cancellationToken);
                if (!recovered)
                {
                    return null;
                }

                continue;
            }

            UserRideOccurrenceCreationOperationDocument? durable =
                await this.LoadCreationOperationAsync(
                    requested.UserId,
                    visitId,
                    requested.OperationKeyHash,
                    requested.ContentMutationFenceToken,
                    cancellationToken);
            if (durable is null)
            {
                return null;
            }

            if (CreationReservationMatchesBatch(durable, requested))
            {
                reservation = durable;
                continue;
            }

            bool normalizationSignalPersisted =
                await this.EnsureCreationNormalizationSignalAsync(
                    durable,
                    visitId,
                    cancellationToken);
            return normalizationSignalPersisted
                ? (durable, false)
                : null;
        }

        return null;
    }

    private async Task<bool> TryActivateCreationReservationAsync(
        UserRideOccurrenceCreationOperationDocument reservation,
        UserRideOccurrenceCreationOperationDocument requested,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(reservation)
            & filters.Eq(
                static document => document.OperationKind,
                CreationKeyReservationOperationKind)
            & filters.Eq(
                static document => document.OperationState,
                ReservedOperationState)
            & filters.Eq(
                static document => document.PayloadHash,
                reservation.PayloadHash)
            & filters.Eq(
                static document => document.VisitId,
                reservation.VisitId);
        UpdateDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> updates =
            Builders<UserRideOccurrenceCreationOperationDocument>.Update;
        List<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>> definitions =
            new List<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>
            {
                updates.Set(
                    static document => document.OperationKind,
                    CreationOperationKind),
                updates.Set(
                    static document => document.OperationState,
                    PendingOperationState),
                updates.Set(
                    static document => document.AppendBaseWasEmpty,
                    requested.AppendBaseWasEmpty),
                updates.Set(
                    static document => document.AppendBaseValidated,
                    false),
                updates.Set(
                    static document => document.WasNormalized,
                    requested.WasNormalized),
                updates.Set(static document => document.Items, requested.Items),
                updates.Set(
                    static document => document.PendingAuditEvents,
                    requested.PendingAuditEvents),
                updates.Set(
                    static document => document.ContentMutationFenceToken,
                    requested.ContentMutationFenceToken),
                updates.Set(static document => document.UpdatedAt, requested.UpdatedAt),
            };
        if (requested.AppendBaseSortPosition.HasValue)
        {
            definitions.Add(updates.Set(
                static document => document.AppendBaseSortPosition,
                requested.AppendBaseSortPosition));
        }
        else
        {
            definitions.Add(updates.Unset(
                static document => document.AppendBaseSortPosition));
        }

        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            filter,
            updates.Combine(definitions),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1;
    }

    private async Task<bool> EnsureCreationNormalizationSignalAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        VisitId visitId,
        CancellationToken cancellationToken)
    {
        if (operation.WasNormalized
            || !string.Equals(
                operation.OperationKind,
                CreationOperationKind,
                StringComparison.Ordinal))
        {
            return true;
        }

        bool wasNormalized = await this.WasCreationOrderNormalizedAsync(
            operation.UserId,
            visitId,
            operation.OperationKeyHash,
            operation.ContentMutationFenceToken,
            cancellationToken);
        if (!wasNormalized)
        {
            return true;
        }

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(operation)
            & filters.Eq(
                static document => document.OperationKind,
                CreationOperationKind);
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
            Builders<UserRideOccurrenceCreationOperationDocument>.Update
                .Set(static document => document.WasNormalized, true);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount != 1)
        {
            return false;
        }

        operation.WasNormalized = true;
        return true;
    }

    private async Task<bool> WasCreationOrderNormalizedAsync(
        string userId,
        VisitId visitId,
        string relatedCreationOperationKeyHash,
        long? contentFenceToken,
        CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.operationCollection
                .Find(UserRideOccurrenceCreationOperationMongoDefinitions
                    .BuildCompletedCreationNormalizationFilter(
                        userId,
                        visitId.Value,
                        relatedCreationOperationKeyHash,
                        contentFenceToken))
                .Limit(1)
                .FirstOrDefaultAsync(cancellationToken);
        return operation is not null
            && string.Equals(
                operation.UserId,
                userId,
                StringComparison.Ordinal)
            && string.Equals(
                operation.VisitId,
                visitId.Value,
                StringComparison.Ordinal)
            && string.Equals(
                operation.OperationKind,
                ReorderOperationKind,
                StringComparison.Ordinal)
            && string.Equals(
                operation.OperationState,
                CompletedOperationState,
                StringComparison.Ordinal)
            && operation.WasNormalized
            && string.Equals(
                operation.RelatedCreationOperationKeyHash,
                relatedCreationOperationKeyHash,
                StringComparison.Ordinal);
    }

    internal static UserRideOccurrenceDocument CreateCreationDocument(
        RideOccurrence occurrence,
        UserRideOccurrenceCreationAllocationDocument allocation,
        string operationKeyHash,
        string payloadHash,
        int operationCount,
        long? contentFenceToken = null)
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
        document.CreationPendingCompletion = contentFenceToken.HasValue ? true : null;
        document.ContentMutationFenceToken = contentFenceToken;
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
            operationCount,
            operation.ContentMutationFenceToken);
    }

    private static UserRideOccurrenceCreationOperationDocument CreateCreationOperation(
        IReadOnlyList<RideOccurrence> occurrences,
        string userId,
        long? contentFenceToken,
        long? expectedLastSortPosition,
        bool wasOrderNormalized,
        string operationKeyHash,
        string payloadHash,
        IReadOnlyCollection<PassportAuditEvent>? pendingAuditEvents)
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
            ContentMutationFenceToken = contentFenceToken,
            OperationState = PendingOperationState,
            AppendBaseWasEmpty = !expectedLastSortPosition.HasValue,
            AppendBaseSortPosition = expectedLastSortPosition,
            WasNormalized = wasOrderNormalized,
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
            PendingAuditEvents = pendingAuditEvents?
                .Select(static auditEvent => auditEvent.ToDocument())
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
        string? relatedCreationOperationKeyHash,
        DateTime operationAtUtc,
        string operationKeyHash,
        string payloadHash,
        IReadOnlyCollection<PassportAuditEvent>? pendingAuditEvents)
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
            ContentMutationFenceToken = request.ContentFenceToken,
            OperationState = PendingOperationState,
            MovedOccurrenceId = request.OccurrenceId.Value,
            ReorderExpectedVersion = request.ExpectedVersion,
            ReorderAnchorOccurrenceId = request.AnchorOccurrenceId?.Value,
            ReorderPlacement = request.Placement,
            WasNormalized = wasNormalized,
            RelatedCreationOperationKeyHash = relatedCreationOperationKeyHash,
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
            PendingAuditEvents = pendingAuditEvents?
                .Select(static auditEvent => auditEvent.ToDocument())
                .ToList(),
            CreatedAt = operationAtUtc,
            UpdatedAt = operationAtUtc,
        };
    }

    private static UpdateDefinition<UserRideOccurrenceDocument> BuildReorderUpdate(
        UserRideOccurrenceReorderAllocationDocument allocation,
        string operationKeyHash,
        long? contentFenceToken)
    {
        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        List<UpdateDefinition<UserRideOccurrenceDocument>> definitions =
            new List<UpdateDefinition<UserRideOccurrenceDocument>>
            {
            updates.Set(
                static document => document.SortPosition,
                allocation.ResultSortPosition),
            updates.Set(static document => document.Version, allocation.ResultVersion),
            updates.Set(
                static document => document.UpdatedAt,
                allocation.ResultUpdatedAtUtc),
            updates.Set(
                static document => document.LastReorderOperationKeyHash,
                operationKeyHash),
            };
        if (contentFenceToken.HasValue)
        {
            definitions.Add(updates.Set(
                static document => document.ContentMutationFenceToken,
                contentFenceToken.Value));
        }

        return updates.Combine(definitions);
    }

    private static void ValidatePendingAuditEvents(
        IReadOnlyCollection<RideOccurrence> occurrences,
        IReadOnlyCollection<PassportAuditEvent>? auditEvents)
    {
        if (auditEvents is null)
        {
            return;
        }

        if (auditEvents.Count != occurrences.Count)
        {
            throw new ArgumentException(
                "Each mutated occurrence must have exactly one pending audit event.",
                nameof(auditEvents));
        }

        IReadOnlyDictionary<string, RideOccurrence> byId = occurrences.ToDictionary(
            static occurrence => occurrence.Id.Value,
            StringComparer.Ordinal);
        foreach (PassportAuditEvent auditEvent in auditEvents)
        {
            if (!byId.TryGetValue(auditEvent.EntityId, out RideOccurrence? occurrence))
            {
                throw new ArgumentException(
                    "A pending audit event does not match the occurrence batch.",
                    nameof(auditEvents));
            }

            ValidatePendingAuditEvent(occurrence, auditEvent);
        }
    }

    private static void ValidatePendingAuditEvent(
        RideOccurrence occurrence,
        PassportAuditEvent auditEvent)
    {
        if (!string.Equals(auditEvent.UserId, occurrence.UserId, StringComparison.Ordinal)
            || !string.Equals(
                auditEvent.VisitId,
                occurrence.VisitId.Value,
                StringComparison.Ordinal)
            || !string.Equals(auditEvent.EntityId, occurrence.Id.Value, StringComparison.Ordinal)
            || auditEvent.EntityType is not (
                PassportAuditEntityType.RideOccurrence or PassportAuditEntityType.RideAssessment)
            || auditEvent.EntityVersion != occurrence.Version)
        {
            throw new ArgumentException(
                "The pending audit event does not match the ride occurrence mutation.",
                nameof(auditEvent));
        }
    }

    private static void ValidateReorderRequest(RideOccurrenceReorderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.VisitId.Value;
        _ = request.OccurrenceId.Value;
        _ = NormalizeRequired(request.UserId, nameof(request.UserId));
        if (request.ExpectedVersion < 1
            || request.ContentFenceToken is <= 0
            || !Enum.IsDefined(request.Placement))
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

    private static RideOccurrenceCreationKeyReservationResult
        CreateCreationKeyReservationResult(
            UserRideOccurrenceCreationOperationDocument? operation,
            RideOccurrenceCreationRequest request,
            string payloadHash,
            RideOccurrenceCreationKeyReservationStatus matchingReservationStatus)
    {
        if (operation is null)
        {
            return new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Missing);
        }

        bool scopeAndPayloadMatch = string.Equals(
                operation.UserId,
                request.UserId,
                StringComparison.Ordinal)
            && string.Equals(
                operation.VisitId,
                request.VisitId.Value,
                StringComparison.Ordinal)
            && string.Equals(operation.PayloadHash, payloadHash, StringComparison.Ordinal);
        if (scopeAndPayloadMatch
            && string.Equals(
                operation.OperationKind,
                CreationKeyReservationOperationKind,
                StringComparison.Ordinal)
            && string.Equals(
                operation.OperationState,
                ReservedOperationState,
                StringComparison.Ordinal)
            && TryCreatePreparation(
                operation.CreationPreparation,
                request.Items.Count,
                out RideOccurrenceCreationPreparation preparation))
        {
            return new RideOccurrenceCreationKeyReservationResult(
                matchingReservationStatus,
                preparation);
        }

        if (scopeAndPayloadMatch
            && string.Equals(
                operation.OperationKind,
                CreationOperationKind,
                StringComparison.Ordinal)
            && operation.OperationState is PendingOperationState
                or CompletedOperationState)
        {
            return new RideOccurrenceCreationKeyReservationResult(
                RideOccurrenceCreationKeyReservationStatus.Finalized);
        }

        return new RideOccurrenceCreationKeyReservationResult(
            RideOccurrenceCreationKeyReservationStatus.Conflict);
    }

    private static bool CreationReservationMatchesBatch(
        UserRideOccurrenceCreationOperationDocument reservation,
        UserRideOccurrenceCreationOperationDocument requested)
    {
        if (!string.Equals(
                reservation.OperationKind,
                CreationKeyReservationOperationKind,
                StringComparison.Ordinal)
            || !string.Equals(
                reservation.OperationState,
                ReservedOperationState,
                StringComparison.Ordinal)
            || !string.Equals(
                reservation.UserId,
                requested.UserId,
                StringComparison.Ordinal)
            || !string.Equals(
                reservation.VisitId,
                requested.VisitId,
                StringComparison.Ordinal)
            || !string.Equals(
                reservation.PayloadHash,
                requested.PayloadHash,
                StringComparison.Ordinal)
            || !TryCreatePreparation(
                reservation.CreationPreparation,
                requested.Items.Count,
                out RideOccurrenceCreationPreparation preparation)
            || !string.Equals(
                preparation.ParkId,
                requested.Items[0].CreationSnapshot.ParkId,
                StringComparison.Ordinal))
        {
            return false;
        }

        for (int index = 0; index < requested.Items.Count; index++)
        {
            if (preparation.HistoricalConsistencies[index]
                != requested.Items[index].CreationSnapshot.HistoricalConsistency)
            {
                return false;
            }
        }

        return true;
    }

    private static UserRideOccurrenceCreationPreparationDocument CreatePreparationDocument(
        RideOccurrenceCreationPreparation preparation)
    {
        return new UserRideOccurrenceCreationPreparationDocument
        {
            ParkId = preparation.ParkId,
            VisitDate = new VisitDateDocument
            {
                Year = preparation.VisitDate.Year,
                Month = preparation.VisitDate.Month,
                Day = preparation.VisitDate.Day,
                Precision = preparation.VisitDate.Precision,
                IsApproximate = preparation.VisitDate.IsApproximate,
            },
            TimeZoneId = preparation.TimeZoneId,
            ServiceDayConvention = preparation.ServiceDayConvention,
            Items = preparation.HistoricalConsistencies
                .Select((consistency, index) =>
                    new UserRideOccurrenceCreationPreparationItemDocument
                    {
                        Index = index,
                        HistoricalConsistency = consistency,
                    })
                .ToList(),
        };
    }

    private static PendingPassportMutationVisit CreatePendingMutation(
        UserRideOccurrenceCreationOperationDocument operation,
        VisitId visitId)
    {
        PendingPassportMutationKind kind = operation.OperationKind switch
        {
            CreationOperationKind => PendingPassportMutationKind.Creation,
            ReorderOperationKind => PendingPassportMutationKind.Reorder,
            DeleteOperationKind => PendingPassportMutationKind.Delete,
            _ => PendingPassportMutationKind.Unknown,
        };
        RideOccurrenceCreationPreparation? preparation = null;
        if (kind == PendingPassportMutationKind.Creation)
        {
            _ = TryCreatePreparation(
                operation.CreationPreparation,
                operation.Items.Count,
                out preparation);
        }

        return new PendingPassportMutationVisit(
            operation.UserId,
            visitId,
            operation.OperationKeyHash,
            kind,
            preparation,
            operation.ContentMutationFenceToken);
    }

    private static bool TryCreatePreparation(
        UserRideOccurrenceCreationPreparationDocument? document,
        int expectedCount,
        out RideOccurrenceCreationPreparation preparation)
    {
        preparation = null!;
        if (document is null
            || string.IsNullOrWhiteSpace(document.ParkId)
            || document.VisitDate is null
            || !Enum.IsDefined(document.ServiceDayConvention)
            || document.Items.Count != expectedCount
            || document.Items.Select(static item => item.Index).Distinct().Count()
                != expectedCount
            || document.Items.Any(item =>
                item.Index is < 0
                || item.Index >= expectedCount
                || !Enum.IsDefined(item.HistoricalConsistency)))
        {
            return false;
        }

        try
        {
            VisitDate visitDate = new VisitDate(
                document.VisitDate.Year,
                document.VisitDate.Month,
                document.VisitDate.Day,
                document.VisitDate.Precision,
                document.VisitDate.IsApproximate);
            preparation = new RideOccurrenceCreationPreparation(
                document.ParkId,
                visitDate,
                document.TimeZoneId,
                document.ServiceDayConvention,
                document.Items
                    .OrderBy(static item => item.Index)
                    .Select(static item => item.HistoricalConsistency)
                    .ToArray());
            return true;
        }
        catch (VisitDateValidationException)
        {
            return false;
        }
    }

    private static void ValidateCreationPreparation(
        RideOccurrenceCreationRequest request,
        RideOccurrenceCreationPreparation preparation)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        _ = preparation.VisitDate;
        _ = NormalizeRequired(preparation.ParkId, nameof(preparation.ParkId));
        if (!Enum.IsDefined(preparation.ServiceDayConvention)
            || preparation.HistoricalConsistencies.Count != request.Items.Count
            || preparation.HistoricalConsistencies.Any(
                static consistency => !Enum.IsDefined(consistency)))
        {
            throw new ArgumentException(
                "The ride occurrence creation preparation is invalid.",
                nameof(preparation));
        }
    }

    private static void ValidateCreationRequest(RideOccurrenceCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.VisitId.Value;
        _ = NormalizeRequired(request.UserId, nameof(request.UserId));
        if (request.Items.Count is < 1 or > MaximumBatchSize
            || request.ContentFenceToken is <= 0
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

    private async Task<CurrentContentFence> LoadCurrentContentFenceAsync(
        string visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (this.visitCollection is null)
        {
            return new CurrentContentFence(true, false, null);
        }

        UserVisitDocument? visit = await this.visitCollection
            .Find(UserVisitMongoDefinitions.BuildOwnedVisitFilter(visitId, userId))
            .FirstOrDefaultAsync(cancellationToken);
        return visit is null
            ? new CurrentContentFence(false, true, null)
            : new CurrentContentFence(
                true,
                visit.ContentMutationFenceToken.HasValue
                    && visit.ContentMutationFenceReady,
                visit.ContentMutationFenceToken,
                visit.ContentMutationFenceStableToken);
    }

    private async Task<IdempotentRideOccurrenceCreationResult>
        EnsureCurrentCreationFenceAsync(
            IdempotentRideOccurrenceCreationResult result,
            RideOccurrenceCreationRequest request,
            CancellationToken cancellationToken)
    {
        if (result.Status is not (
                IdempotentRideOccurrenceCreationStatus.Created
                or IdempotentRideOccurrenceCreationStatus.Replayed)
            || !request.ContentFenceToken.HasValue
            || this.visitCollection is null)
        {
            return result;
        }

        CurrentContentFence current = await this.LoadCurrentContentFenceAsync(
            request.VisitId.Value,
            request.UserId,
            cancellationToken);
        return current.VisitExists
            && current.IsEnforced
            && current.Token == request.ContentFenceToken
                ? result
                : CreateCreationConcurrencyConflictResult();
    }

    private static FilterDefinition<UserRideOccurrenceDocument> WithCurrentContentFence(
        FilterDefinition<UserRideOccurrenceDocument> filter,
        CurrentContentFence currentFence)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        if (!currentFence.Token.HasValue)
        {
            return filter & filters.Or(
                filters.Exists(
                    static document => document.ContentMutationFenceToken,
                    false),
                filters.Eq(
                    static document => document.ContentMutationFenceToken,
                    null));
        }

        if (currentFence.IsEnforced)
        {
            return filter & filters.Eq(
                static document => document.ContentMutationFenceToken,
                currentFence.Token.Value);
        }

        FilterDefinition<UserRideOccurrenceDocument> safeFence =
            currentFence.StableToken.HasValue
                ? filters.Gte(
                        static document => document.ContentMutationFenceToken,
                        currentFence.StableToken.Value)
                    & filters.Lte(
                        static document => document.ContentMutationFenceToken,
                        currentFence.Token.Value)
                : filters.Or(
                filters.Exists(
                    static document => document.ContentMutationFenceToken,
                    false),
                filters.Eq(
                    static document => document.ContentMutationFenceToken,
                    null),
                filters.Gte(
                        static document => document.ContentMutationFenceToken,
                        1L)
                    & filters.Lte(
                        static document => document.ContentMutationFenceToken,
                        currentFence.Token.Value));
        return filter & safeFence;
    }

    private static IMongoCollection<TDocument> GetCollection<TDocument>(
        IMongoDatabase database,
        MongoDbSettings settings,
        Func<MongoDbSettings, string> collectionName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(collectionName);
        return database.GetCollection<TDocument>(collectionName(settings));
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

    private static long ValidateContentFence(long value, string parameterName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private sealed record BatchScope(string UserId, VisitId VisitId);

    private sealed record CurrentContentFence(
        bool VisitExists,
        bool IsEnforced,
        long? Token,
        long? StableToken = null)
    {
        public bool Matches(long? token)
        {
            if (!this.Token.HasValue)
            {
                return !token.HasValue;
            }

            if (this.IsEnforced)
            {
                return token == this.Token;
            }

            return this.StableToken.HasValue
                ? token >= this.StableToken && token <= this.Token
                : !token.HasValue || token is >= 1 && token <= this.Token;
        }
    }
}
