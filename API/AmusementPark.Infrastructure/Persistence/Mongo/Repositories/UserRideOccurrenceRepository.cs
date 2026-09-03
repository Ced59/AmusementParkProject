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

    public UserRideOccurrenceRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        this.collection = database.GetCollection<UserRideOccurrenceDocument>(
            settings.UserRideOccurrencesCollectionName);
        this.operationCollection =
            database.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                settings.UserRideOccurrenceOperationsCollectionName);
    }

    public async Task<IdempotentRideOccurrenceCreationResult?> ResolveExistingBatchCreationAsync(
        IReadOnlyList<RideOccurrence> requestedOccurrences,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        BatchScope scope = ValidateBatch(requestedOccurrences);
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(
            requestedOccurrences);
        UserRideOccurrenceCreationOperationDocument? operation =
            await this.LoadCreationOperationAsync(
                scope.UserId,
                operationKeyHash,
                cancellationToken);
        if (operation is null)
        {
            return null;
        }

        List<UserRideOccurrenceDocument> existing = await this.LoadCreationDocumentsAsync(
            scope.UserId,
            operationKeyHash,
            cancellationToken);
        return ResolveAgainstOperation(
            operation,
            existing,
            payloadHash,
            requestedOccurrences.Count);
    }

    public async Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAsync(
        IReadOnlyList<RideOccurrence> occurrences,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        BatchScope scope = ValidateBatch(occurrences);
        string operationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
            NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(occurrences);
        (UserRideOccurrenceCreationOperationDocument operation, bool isNewOperation) =
            await this.EnsureCreationOperationAsync(
                occurrences,
                scope.UserId,
                operationKeyHash,
                payloadHash,
                cancellationToken);
        if (!OperationMatches(operation, payloadHash, occurrences.Count))
        {
            return CreateConflictResult();
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
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ValidateReorderRequest(request);
        ValidateReorderChanges(request, changes, resultOccurrence);
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
                return CreateReorderConflictResult(wasNormalized);
            }

            operation = existing;
            wasExisting = true;
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
        if (!OperationMatches(operation, payloadHash, expectedCount))
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

    private async Task<IdempotentRideOccurrenceReorderResult> ApplyReorderOperationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        RideOccurrenceReorderRequest request,
        string operationKeyHash,
        bool wasExisting,
        CancellationToken cancellationToken)
    {
        string payloadHash =
            UserRideOccurrenceCreationFingerprint.HashReorderPayload(request);
        if (!ReorderOperationMatches(operation, request, payloadHash))
        {
            return CreateReorderConflictResult(operation.WasNormalized);
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
            foreach (UserRideOccurrenceReorderAllocationDocument allocation in
                operation.ReorderItems!.OrderBy(static item => item.Index))
            {
                UpdateResult update = await this.collection.UpdateOneAsync(
                    UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                        allocation.OccurrenceId,
                        request.VisitId.Value,
                        request.UserId,
                        allocation.ExpectedVersion),
                    BuildReorderUpdate(allocation.ResultSnapshot, operationKeyHash),
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
                    && ReorderAllocationWasApplied(
                        current,
                        allocation,
                        operationKeyHash))
                {
                    continue;
                }

                await this.SetReorderOperationStateAsync(
                    operation,
                    ConflictOperationState,
                    cancellationToken);
                return CreateReorderConflictResult(operation.WasNormalized);
            }

            await this.SetReorderOperationStateAsync(
                operation,
                CompletedOperationState,
                cancellationToken);
            operation.OperationState = CompletedOperationState;
        }

        RideOccurrence occurrence = operation.ReorderResultSnapshot!.SnapshotToDomain(
            operation.MovedOccurrenceId!,
            operation.UserId);
        return new IdempotentRideOccurrenceReorderResult(
            wasExisting
                ? IdempotentRideOccurrenceReorderStatus.Replayed
                : IdempotentRideOccurrenceReorderStatus.Applied,
            occurrence,
            operation.WasNormalized);
    }

    private async Task SetReorderOperationStateAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        string state,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update =
            Builders<UserRideOccurrenceCreationOperationDocument>.Update
                .Set(static document => document.OperationState, state)
                .Set(static document => document.UpdatedAt, operation.UpdatedAt);
        await this.operationCollection.UpdateOneAsync(
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash),
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
    }

    private async Task<(UserRideOccurrenceCreationOperationDocument Operation, bool IsNew)>
        EnsureCreationOperationAsync(
            IReadOnlyList<RideOccurrence> occurrences,
            string userId,
            string operationKeyHash,
            string payloadHash,
            CancellationToken cancellationToken)
    {
        UserRideOccurrenceCreationOperationDocument requested =
            CreateCreationOperation(
                occurrences,
                userId,
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
            if (existing is null)
            {
                throw;
            }

            return (existing, false);
        }
    }

    internal static UserRideOccurrenceDocument CreateCreationDocument(
        RideOccurrence occurrence,
        UserRideOccurrenceCreationAllocationDocument allocation,
        string operationKeyHash,
        string payloadHash,
        int operationCount)
    {
        UserRideOccurrenceDocument document = occurrence.ToDocument();
        document.Id = allocation.OccurrenceId;
        document.SortPosition = allocation.SortPosition;
        document.CreatedAt = allocation.CreatedAtUtc;
        document.UpdatedAt = allocation.UpdatedAtUtc;
        document.CreationOperationKeyHash = operationKeyHash;
        document.CreationPayloadHash = payloadHash;
        document.CreationOperationIndex = allocation.Index;
        document.CreationOperationCount = operationCount;
        document.CreationSnapshot = document.CreateCreationSnapshot();
        return document;
    }

    private static UserRideOccurrenceCreationOperationDocument CreateCreationOperation(
        IReadOnlyList<RideOccurrence> occurrences,
        string userId,
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
            Items = occurrences
                .Select((occurrence, index) =>
                    new UserRideOccurrenceCreationAllocationDocument
                    {
                        Index = index,
                        OccurrenceId = occurrence.Id.Value,
                        SortPosition = occurrence.SortPosition,
                        CreatedAtUtc = ToMongoPrecision(occurrence.CreatedAtUtc),
                        UpdatedAtUtc = ToMongoPrecision(occurrence.UpdatedAtUtc),
                    })
                .ToList(),
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
        };
    }

    private static UserRideOccurrenceCreationOperationDocument CreateReorderOperation(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
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
                        ResultSnapshot = document.CreateCreationSnapshot(),
                    };
                })
                .ToList(),
            ReorderResultSnapshot = resultDocument.CreateCreationSnapshot(),
            CreatedAt = operationAtUtc,
            UpdatedAt = operationAtUtc,
        };
    }

    private static bool OperationMatches(
        UserRideOccurrenceCreationOperationDocument operation,
        string payloadHash,
        int expectedCount)
    {
        if (!string.Equals(operation.PayloadHash, payloadHash, StringComparison.Ordinal)
            || operation.Items.Count != expectedCount)
        {
            return false;
        }

        int distinctIndexes = operation.Items
            .Select(static item => item.Index)
            .Distinct()
            .Count();
        int distinctIds = operation.Items
            .Select(static item => item.OccurrenceId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        int distinctPositions = operation.Items
            .Select(static item => item.SortPosition)
            .Distinct()
            .Count();
        return distinctIndexes == expectedCount
            && distinctIds == expectedCount
            && distinctPositions == expectedCount
            && operation.Items.All(item =>
                item.Index is >= 0
                && item.Index < expectedCount
                && !string.IsNullOrWhiteSpace(item.OccurrenceId)
                && item.CreatedAtUtc.Kind == DateTimeKind.Utc
                && item.UpdatedAtUtc.Kind == DateTimeKind.Utc
                && item.UpdatedAtUtc >= item.CreatedAtUtc);
    }

    private static bool ReorderOperationMatches(
        UserRideOccurrenceCreationOperationDocument operation,
        RideOccurrenceReorderRequest request,
        string payloadHash)
    {
        return string.Equals(operation.OperationKind, ReorderOperationKind, StringComparison.Ordinal)
            && string.Equals(operation.PayloadHash, payloadHash, StringComparison.Ordinal)
            && string.Equals(operation.VisitId, request.VisitId.Value, StringComparison.Ordinal)
            && string.Equals(operation.UserId, request.UserId, StringComparison.Ordinal)
            && string.Equals(
                operation.MovedOccurrenceId,
                request.OccurrenceId.Value,
                StringComparison.Ordinal)
            && operation.ReorderItems is not null
            && operation.ReorderItems.Count <= RideOccurrenceOrderPlanner.MaximumReorderSize
            && operation.ReorderItems.Select(static item => item.Index).Distinct().Count()
                == operation.ReorderItems.Count
            && operation.ReorderItems.Select(static item => item.OccurrenceId).Distinct(StringComparer.Ordinal).Count()
                == operation.ReorderItems.Count
            && operation.ReorderResultSnapshot is not null;
    }

    private static UpdateDefinition<UserRideOccurrenceDocument> BuildReorderUpdate(
        UserRideOccurrenceCreationSnapshotDocument snapshot,
        string operationKeyHash)
    {
        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        return updates.Combine(
            updates.Set(static document => document.SortPosition, snapshot.SortPosition),
            updates.Set(static document => document.Version, snapshot.Version),
            updates.Set(static document => document.UpdatedAt, snapshot.UpdatedAtUtc),
            updates.Set(
                static document => document.LastReorderOperationKeyHash,
                operationKeyHash));
    }

    private static bool ReorderAllocationWasApplied(
        UserRideOccurrenceDocument current,
        UserRideOccurrenceReorderAllocationDocument allocation,
        string operationKeyHash)
    {
        return string.Equals(
                current.LastReorderOperationKeyHash,
                operationKeyHash,
                StringComparison.Ordinal)
            && current.SortPosition == allocation.ResultSnapshot.SortPosition
            && current.Version == allocation.ResultSnapshot.Version
            && current.UpdatedAt == allocation.ResultSnapshot.UpdatedAtUtc;
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
        RideOccurrence resultOccurrence)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(resultOccurrence);
        if (changes.Count > RideOccurrenceOrderPlanner.MaximumReorderSize
            || resultOccurrence.Id != request.OccurrenceId
            || resultOccurrence.VisitId != request.VisitId
            || !string.Equals(resultOccurrence.UserId, request.UserId, StringComparison.Ordinal)
            || changes.Any(change =>
                change.ExpectedVersion < 1
                || change.Occurrence.VisitId != request.VisitId
                || !string.Equals(change.Occurrence.UserId, request.UserId, StringComparison.Ordinal)
                || change.Occurrence.Version != change.ExpectedVersion + 1))
        {
            throw new ArgumentException("The reorder plan is invalid.", nameof(changes));
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

    private static IdempotentRideOccurrenceCreationResult CreateConflictResult()
    {
        return new IdempotentRideOccurrenceCreationResult(
            IdempotentRideOccurrenceCreationStatus.Conflict,
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

        return new BatchScope(first.UserId);
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

    private sealed record BatchScope(string UserId);
}
