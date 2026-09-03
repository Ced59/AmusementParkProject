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

    private const string CreationKeyReservationOperationKind =
        "creation-key-reservation";

    private const string ReorderOperationKind = "reorder";

    private const string PendingOperationState = "pending";

    private const string CompletedOperationState = "completed";

    private const string ConflictOperationState = "conflict";

    private const string ReservedOperationState = "reserved";

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
                operationKeyHash,
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
                    operationKeyHash,
                    cancellationToken);
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
                operationKeyHash,
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
        bool wasOrderNormalized,
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
                wasOrderNormalized,
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
                created,
                operation.WasNormalized);
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
        UserRideOccurrenceDocument? lastOccurrence = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildActiveVisitFilter(
                visitId.Value,
                normalizedUserId))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildReverseVisitOrderSort())
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        bool wasNormalized = await this.WasCreationOrderNormalizedAsync(
            normalizedUserId,
            visitId,
            relatedCreationOperationKeyHash,
            cancellationToken);
        return new RideOccurrenceAppendState(
            lastOccurrence?.SortPosition,
            wasNormalized);
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
        string? relatedCreationClientOperationId,
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

        IdempotentRideOccurrenceCreationResult? result =
            ResolveIdempotentBatchCreation(existing, payloadHash, expectedCount);
        return result is null
            ? null
            : result with { WasNormalized = operation.WasNormalized };
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
                operationKeyHash,
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
                operationKeyHash,
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
            long? expectedLastSortPosition,
            bool wasOrderNormalized,
            string operationKeyHash,
            string payloadHash,
            CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument requested =
            CreateCreationOperation(
                occurrences,
                userId,
                expectedLastSortPosition,
                wasOrderNormalized,
                operationKeyHash,
                payloadHash);
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
                    operationKeyHash,
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
                    operationKeyHash,
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
                    requested.OperationKeyHash,
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
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                reservation.UserId,
                reservation.OperationKeyHash)
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
            cancellationToken);
        if (!wasNormalized)
        {
            return true;
        }

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
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
        CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.operationCollection
                .Find(UserRideOccurrenceCreationOperationMongoDefinitions
                    .BuildCompletedCreationNormalizationFilter(
                        userId,
                        visitId.Value,
                        relatedCreationOperationKeyHash))
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
        bool wasOrderNormalized,
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
