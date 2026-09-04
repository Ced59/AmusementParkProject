using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class UserRideOccurrenceProvisionalCreationReconciler
{
    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;
    private readonly IMongoCollection<UserVisitDocument>? visitCollection;

    public UserRideOccurrenceProvisionalCreationReconciler(
        IMongoCollection<UserRideOccurrenceDocument> collection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operationCollection,
        IMongoCollection<UserVisitDocument>? visitCollection = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(operationCollection);
        this.collection = collection;
        this.operationCollection = operationCollection;
        this.visitCollection = visitCollection;
    }

    public async Task<int> ReconcileBatchAsync(
        int maximumDocumentCount,
        CancellationToken cancellationToken)
    {
        if (maximumDocumentCount is < 1
            or > UserRideOccurrenceRepository.MaximumListSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDocumentCount));
        }

        List<UserRideOccurrenceDocument> documents = await this.collection
            .Find(BuildPendingCompletionFilter())
            .Sort(BuildPendingCompletionSort())
            .Limit(maximumDocumentCount)
            .ToListAsync(cancellationToken);
        int reconciledCount = 0;
        foreach (UserRideOccurrenceDocument document in documents)
        {
            UserRideOccurrenceCreationOperationDocument? operation =
                await this.LoadOperationAsync(document, cancellationToken);
            if (OperationFenceMayBePromoting(document, operation))
            {
                UserVisitDocument? visit = await this.LoadVisitAsync(
                    document,
                    cancellationToken);
                if (IsInsideIncompletePromotion(document, operation!, visit))
                {
                    continue;
                }

                operation = await this.LoadOperationAsync(document, cancellationToken);
            }

            ProvisionalCreationDisposition disposition = ResolveDisposition(
                document,
                operation);
            if (disposition == ProvisionalCreationDisposition.Wait)
            {
                continue;
            }

            FilterDefinition<UserRideOccurrenceDocument> exactDocument =
                BuildExactPendingDocumentFilter(document);
            if (disposition == ProvisionalCreationDisposition.Commit)
            {
                UpdateResult result = await this.collection.UpdateOneAsync(
                    exactDocument,
                    Builders<UserRideOccurrenceDocument>.Update.Unset(
                        static value => value.CreationPendingCompletion),
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken);
                if (result.ModifiedCount == 1)
                {
                    reconciledCount++;
                }

                continue;
            }

            DeleteResult deletion = await this.collection.DeleteOneAsync(
                exactDocument,
                cancellationToken);
            if (deletion.DeletedCount == 1)
            {
                reconciledCount++;
            }
        }

        return reconciledCount;
    }

    private async Task<UserRideOccurrenceCreationOperationDocument?> LoadOperationAsync(
        UserRideOccurrenceDocument document,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.CreationOperationKeyHash)
            || string.IsNullOrWhiteSpace(document.VisitId)
            || string.IsNullOrWhiteSpace(document.UserId))
        {
            return null;
        }

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return await this.operationCollection
            .Find(UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                    document.UserId,
                    document.CreationOperationKeyHash)
                & filters.Eq(static operation => operation.VisitId, document.VisitId))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<UserVisitDocument?> LoadVisitAsync(
        UserRideOccurrenceDocument document,
        CancellationToken cancellationToken)
    {
        if (this.visitCollection is null
            || string.IsNullOrWhiteSpace(document.VisitId)
            || string.IsNullOrWhiteSpace(document.UserId))
        {
            return null;
        }

        return await this.visitCollection
            .Find(UserVisitMongoDefinitions.BuildOwnedVisitFilter(
                document.VisitId,
                document.UserId))
            .Project<UserVisitDocument>(Builders<UserVisitDocument>.Projection
                .Include(static visit => visit.Id)
                .Include(static visit => visit.UserId)
                .Include(static visit => visit.ContentMutationFenceToken)
                .Include(static visit => visit.ContentMutationFenceStableToken)
                .Include(static visit => visit.ContentMutationFenceReady))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool OperationFenceMayBePromoting(
        UserRideOccurrenceDocument document,
        UserRideOccurrenceCreationOperationDocument? operation)
    {
        return MatchesOperationAllocation(document, operation)
            && operation!.OperationState is "completed" or "pending"
            && document.ContentMutationFenceToken.HasValue
            && (!operation.ContentMutationFenceToken.HasValue
                || document.ContentMutationFenceToken.Value
                    > operation.ContentMutationFenceToken.Value);
    }

    private static bool IsInsideIncompletePromotion(
        UserRideOccurrenceDocument document,
        UserRideOccurrenceCreationOperationDocument operation,
        UserVisitDocument? visit)
    {
        return visit is not null
            && !visit.ContentMutationFenceReady
            && FenceBelongsToSafeInterval(
                visit,
                document.ContentMutationFenceToken)
            && FenceBelongsToSafeInterval(
                visit,
                operation.ContentMutationFenceToken);
    }

    private static bool FenceBelongsToSafeInterval(
        UserVisitDocument visit,
        long? sourceFence)
    {
        if (!visit.ContentMutationFenceToken.HasValue)
        {
            return !sourceFence.HasValue;
        }

        return visit.ContentMutationFenceStableToken.HasValue
            ? sourceFence >= visit.ContentMutationFenceStableToken
                && sourceFence <= visit.ContentMutationFenceToken
            : !sourceFence.HasValue
                || sourceFence is >= 1
                    && sourceFence <= visit.ContentMutationFenceToken;
    }

    private static ProvisionalCreationDisposition ResolveDisposition(
        UserRideOccurrenceDocument document,
        UserRideOccurrenceCreationOperationDocument? operation)
    {
        if (!MatchesOperationAllocation(document, operation))
        {
            return ProvisionalCreationDisposition.Delete;
        }

        UserRideOccurrenceCreationOperationDocument exactOperation = operation!;
        if (string.Equals(
            exactOperation.OperationState,
            "completed",
            StringComparison.Ordinal))
        {
            return exactOperation.ContentMutationFenceToken
                == document.ContentMutationFenceToken
                ? ProvisionalCreationDisposition.Commit
                : ProvisionalCreationDisposition.Delete;
        }

        if (string.Equals(
                exactOperation.OperationState,
                "pending",
                StringComparison.Ordinal)
            && exactOperation.ContentMutationFenceToken.HasValue
            && document.ContentMutationFenceToken.HasValue
            && exactOperation.ContentMutationFenceToken.Value
                >= document.ContentMutationFenceToken.Value)
        {
            return ProvisionalCreationDisposition.Wait;
        }

        return ProvisionalCreationDisposition.Delete;
    }

    private static bool MatchesOperationAllocation(
        UserRideOccurrenceDocument document,
        UserRideOccurrenceCreationOperationDocument? operation)
    {
        if (operation is null
            || document.CreationOperationCount is not >= 1
            || !document.CreationOperationIndex.HasValue
            || !UserRideOccurrenceOperationValidator.CreationMatches(
                operation,
                document.CreationPayloadHash ?? string.Empty,
                document.CreationOperationCount.Value))
        {
            return false;
        }

        return string.Equals(operation.UserId, document.UserId, StringComparison.Ordinal)
            && string.Equals(operation.VisitId, document.VisitId, StringComparison.Ordinal)
            && operation.Items.Any(allocation =>
                allocation.Index == document.CreationOperationIndex.Value
                && string.Equals(
                    allocation.OccurrenceId,
                    document.Id,
                    StringComparison.Ordinal));
    }

    private static FilterDefinition<UserRideOccurrenceDocument>
        BuildPendingCompletionFilter()
    {
        return Builders<UserRideOccurrenceDocument>.Filter.Eq(
            static document => document.CreationPendingCompletion,
            true);
    }

    private static SortDefinition<UserRideOccurrenceDocument> BuildPendingCompletionSort()
    {
        return Builders<UserRideOccurrenceDocument>.Sort
            .Ascending(static document => document.CreatedAt)
            .Ascending(static document => document.Id);
    }

    private static FilterDefinition<UserRideOccurrenceDocument>
        BuildExactPendingDocumentFilter(UserRideOccurrenceDocument document)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(static value => value.Id, document.Id)
            & filters.Eq(static value => value.UserId, document.UserId)
            & filters.Eq(static value => value.VisitId, document.VisitId)
            & filters.Eq(
                static value => value.CreationOperationKeyHash,
                document.CreationOperationKeyHash)
            & filters.Eq(
                static value => value.CreationPayloadHash,
                document.CreationPayloadHash)
            & filters.Eq(
                static value => value.CreationOperationIndex,
                document.CreationOperationIndex)
            & filters.Eq(
                static value => value.CreationOperationCount,
                document.CreationOperationCount)
            & filters.Eq(
                static value => value.ContentMutationFenceToken,
                document.ContentMutationFenceToken)
            & BuildPendingCompletionFilter();
    }

    private enum ProvisionalCreationDisposition
    {
        Wait = 1,
        Commit = 2,
        Delete = 3,
    }
}
