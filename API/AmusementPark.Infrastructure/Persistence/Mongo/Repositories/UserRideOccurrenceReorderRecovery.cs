using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class UserRideOccurrenceReorderRecovery
{
    private const int MaximumRollbackAttempts = 5;

    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;

    public UserRideOccurrenceReorderRecovery(
        IMongoCollection<UserRideOccurrenceDocument> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        this.collection = collection;
    }

    public async Task<bool> TryRollbackAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyList<UserRideOccurrenceReorderAllocationDocument> allocations,
        string operationKeyHash,
        CancellationToken cancellationToken)
    {
        foreach (UserRideOccurrenceReorderAllocationDocument allocation in
            allocations.Reverse())
        {
            bool restored = false;
            for (int attempt = 0; attempt < MaximumRollbackAttempts && !restored; attempt++)
            {
                UserRideOccurrenceDocument? current = await this.collection
                    .Find(UserRideOccurrenceMongoDefinitions.WithContentFence(
                        UserRideOccurrenceMongoDefinitions.BuildOwnedAnyStateFilter(
                            allocation.OccurrenceId,
                            request.VisitId.Value,
                            request.UserId),
                        request.ContentFenceToken))
                    .FirstOrDefaultAsync(cancellationToken);
                if (current is null)
                {
                    return false;
                }

                if (current.SortPosition == allocation.PreviousSortPosition
                    && !string.Equals(
                        current.LastReorderOperationKeyHash,
                        operationKeyHash,
                        StringComparison.Ordinal))
                {
                    restored = true;
                    continue;
                }

                if (!string.Equals(
                        current.LastReorderOperationKeyHash,
                        operationKeyHash,
                        StringComparison.Ordinal)
                    || current.Version == long.MaxValue)
                {
                    return false;
                }

                UpdateResult result = await this.collection.UpdateOneAsync(
                    UserRideOccurrenceMongoDefinitions.WithContentFence(
                        UserRideOccurrenceMongoDefinitions
                            .BuildOwnedAnyStateReorderVersionFilter(
                                allocation.OccurrenceId,
                                request.VisitId.Value,
                                request.UserId,
                                current.Version,
                                operationKeyHash),
                        request.ContentFenceToken),
                    BuildRollbackUpdate(
                        current,
                        allocation,
                        request.ContentFenceToken),
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken);
                restored = result.MatchedCount == 1;
            }

            if (!restored)
            {
                return false;
            }
        }

        return true;
    }

    public static bool AllocationWasApplied(
        UserRideOccurrenceDocument current,
        UserRideOccurrenceReorderAllocationDocument allocation,
        string operationKeyHash)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(allocation);
        return string.Equals(
                current.LastReorderOperationKeyHash,
                operationKeyHash,
                StringComparison.Ordinal)
            && current.SortPosition == allocation.ResultSortPosition
            && current.Version >= allocation.ResultVersion
            && current.UpdatedAt >= allocation.ResultUpdatedAtUtc;
    }

    public static UpdateDefinition<UserRideOccurrenceDocument> BuildRollbackUpdate(
        UserRideOccurrenceDocument current,
        UserRideOccurrenceReorderAllocationDocument allocation,
        long? contentFenceToken = null)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(allocation);
        if (current.Version == long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(current),
                "The ride occurrence version cannot be incremented.");
        }

        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        List<UpdateDefinition<UserRideOccurrenceDocument>> definitions =
            new List<UpdateDefinition<UserRideOccurrenceDocument>>
            {
            updates.Set(
                static document => document.SortPosition,
                allocation.PreviousSortPosition),
            updates.Set(static document => document.Version, current.Version + 1),
            updates.Set(static document => document.UpdatedAt, current.UpdatedAt),
            updates.Unset(static document => document.LastReorderOperationKeyHash),
            };
        if (contentFenceToken.HasValue)
        {
            definitions.Add(updates.Set(
                static document => document.ContentMutationFenceToken,
                contentFenceToken));
        }

        return updates.Combine(definitions);
    }
}
