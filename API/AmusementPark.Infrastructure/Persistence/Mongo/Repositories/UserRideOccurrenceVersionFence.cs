using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class UserRideOccurrenceVersionFence
{
    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;

    public UserRideOccurrenceVersionFence(
        IMongoCollection<UserRideOccurrenceDocument> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        this.collection = collection;
    }

    public async Task<bool> TryConfirmOwnedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        long expectedVersion,
        long? contentFenceToken,
        CancellationToken cancellationToken)
    {
        UpdateResult update = await this.collection.UpdateOneAsync(
            UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                    occurrenceId.Value,
                    visitId.Value,
                    userId,
                    expectedVersion),
                contentFenceToken),
            BuildConfirmationUpdate(expectedVersion, contentFenceToken),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return update.MatchedCount == 1;
    }

    public async Task<bool> TryApplyUnchangedReorderAsync(
        RideOccurrenceReorderRequest request,
        string operationKeyHash,
        CancellationToken cancellationToken)
    {
        UpdateResult update = await this.collection.UpdateOneAsync(
            UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                    request.OccurrenceId.Value,
                    request.VisitId.Value,
                    request.UserId,
                    request.ExpectedVersion),
                request.ContentFenceToken),
            BuildUnchangedReorderUpdate(request, operationKeyHash),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (update.MatchedCount == 1)
        {
            return true;
        }

        UserRideOccurrenceDocument? current = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.WithContentFence(
                UserRideOccurrenceMongoDefinitions.BuildOwnedAnyStateFilter(
                    request.OccurrenceId.Value,
                    request.VisitId.Value,
                    request.UserId),
                request.ContentFenceToken))
            .FirstOrDefaultAsync(cancellationToken);
        return current is not null
            && string.Equals(
                current.LastReorderOperationKeyHash,
                operationKeyHash,
                StringComparison.Ordinal);
    }

    private static UpdateDefinition<UserRideOccurrenceDocument> BuildConfirmationUpdate(
        long expectedVersion,
        long? contentFenceToken)
    {
        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        return !contentFenceToken.HasValue
            ? updates.Set(static document => document.Version, expectedVersion)
            : updates.Combine(
                updates.Set(static document => document.Version, expectedVersion),
                updates.Set(
                    static document => document.ContentMutationFenceToken,
                    contentFenceToken.Value));
    }

    private static UpdateDefinition<UserRideOccurrenceDocument> BuildUnchangedReorderUpdate(
        RideOccurrenceReorderRequest request,
        string operationKeyHash)
    {
        UpdateDefinitionBuilder<UserRideOccurrenceDocument> updates =
            Builders<UserRideOccurrenceDocument>.Update;
        return !request.ContentFenceToken.HasValue
            ? updates.Set(
                static document => document.LastReorderOperationKeyHash,
                operationKeyHash)
            : updates.Combine(
                updates.Set(
                    static document => document.LastReorderOperationKeyHash,
                    operationKeyHash),
                updates.Set(
                    static document => document.ContentMutationFenceToken,
                    request.ContentFenceToken.Value));
    }
}
