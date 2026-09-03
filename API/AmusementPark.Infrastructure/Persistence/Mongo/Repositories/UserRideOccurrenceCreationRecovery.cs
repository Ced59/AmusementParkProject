using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class UserRideOccurrenceCreationRecovery
{
    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;

    public UserRideOccurrenceCreationRecovery(
        IMongoCollection<UserRideOccurrenceDocument> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        this.collection = collection;
    }

    public async Task<IdempotentRideOccurrenceCreationResult> RecoverAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        IReadOnlyCollection<UserRideOccurrenceDocument> existing,
        string payloadHash,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserRideOccurrenceDocument> recoverable = existing;
        if (operation.ContentMutationFenceToken.HasValue)
        {
            await this.AdoptLateDocumentsAsync(
                operation,
                payloadHash,
                expectedCount,
                cancellationToken);
            recoverable = await this.LoadCurrentDocumentsAsync(
                operation,
                cancellationToken);
        }

        HashSet<int> existingIndexes = recoverable
            .Where(static document => document.CreationOperationIndex.HasValue)
            .Select(static document => document.CreationOperationIndex!.Value)
            .ToHashSet();
        List<UserRideOccurrenceDocument> missing = operation.Items
            .Where(allocation => !existingIndexes.Contains(allocation.Index))
            .OrderBy(static allocation => allocation.Index)
            .Select(allocation => UserRideOccurrenceRepository
                .CreateDocumentFromCreationAllocation(
                    operation,
                    allocation,
                    payloadHash,
                    expectedCount))
            .ToList();
        if (missing.Count > 0)
        {
            try
            {
                await this.collection.InsertManyAsync(
                    missing,
                    new InsertManyOptions { IsOrdered = false },
                    cancellationToken);
            }
            catch (MongoBulkWriteException<UserRideOccurrenceDocument> exception)
                when (ContainsOnlyDuplicateKeyErrors(exception))
            {
                // Une autre reprise de la même clé peut avoir terminé les allocations.
                if (operation.ContentMutationFenceToken.HasValue)
                {
                    await this.AdoptLateDocumentsAsync(
                        operation,
                        payloadHash,
                        expectedCount,
                        cancellationToken);
                }
            }

            recoverable = await this.LoadCurrentDocumentsAsync(
                operation,
                cancellationToken);
        }

        return UserRideOccurrenceRepository.ResolveAgainstOperation(
                operation,
                recoverable,
                payloadHash,
                expectedCount)
            ?? new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Conflict,
                Array.Empty<RideOccurrence>());
    }

    private async Task AdoptLateDocumentsAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        string payloadHash,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        long currentFenceToken = operation.ContentMutationFenceToken!.Value;
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
                currentFenceToken));
        FilterDefinition<UserRideOccurrenceDocument> exactAllocation =
            UserRideOccurrenceMongoDefinitions.BuildCreationOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.VisitId, operation.VisitId)
            & filters.Eq(static document => document.CreationPayloadHash, payloadHash)
            & filters.Eq(static document => document.CreationOperationCount, expectedCount)
            & filters.In(
                static document => document.Id,
                operation.Items.Select(static item => item.OccurrenceId));
        _ = await this.collection.UpdateManyAsync(
            exactAllocation & olderFence,
            Builders<UserRideOccurrenceDocument>.Update.Set(
                static document => document.ContentMutationFenceToken,
                currentFenceToken),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
    }

    private async Task<List<UserRideOccurrenceDocument>> LoadCurrentDocumentsAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        return await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildCreationOperationFilter(
                    operation.UserId,
                    operation.OperationKeyHash),
                operation.ContentMutationFenceToken))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildCreationOperationSort())
            .ToListAsync(cancellationToken);
    }

    private static bool ContainsOnlyDuplicateKeyErrors(
        MongoBulkWriteException<UserRideOccurrenceDocument> exception)
    {
        return exception.WriteConcernError is null
            && exception.WriteErrors.Count > 0
            && exception.WriteErrors.All(
                static error => error.Category == ServerErrorCategory.DuplicateKey);
    }
}
