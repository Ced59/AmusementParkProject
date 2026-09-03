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
        HashSet<int> existingIndexes = existing
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
            }
        }

        List<UserRideOccurrenceDocument> recovered = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildCreationOperationFilter(
                    operation.UserId,
                    operation.OperationKeyHash),
                operation.ContentMutationFenceToken))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildCreationOperationSort())
            .ToListAsync(cancellationToken);
        return UserRideOccurrenceRepository.ResolveAgainstOperation(
                operation,
                recovered,
                payloadHash,
                expectedCount)
            ?? new IdempotentRideOccurrenceCreationResult(
                IdempotentRideOccurrenceCreationStatus.Conflict,
                Array.Empty<RideOccurrence>());
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
