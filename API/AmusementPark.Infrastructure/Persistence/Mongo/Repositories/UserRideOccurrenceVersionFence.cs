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
        CancellationToken cancellationToken)
    {
        UpdateResult update = await this.collection.UpdateOneAsync(
            UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                occurrenceId.Value,
                visitId.Value,
                userId,
                expectedVersion),
            Builders<UserRideOccurrenceDocument>.Update.Set(
                static document => document.Version,
                expectedVersion),
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
            UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                request.OccurrenceId.Value,
                request.VisitId.Value,
                request.UserId,
                request.ExpectedVersion),
            Builders<UserRideOccurrenceDocument>.Update.Set(
                static document => document.LastReorderOperationKeyHash,
                operationKeyHash),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (update.MatchedCount == 1)
        {
            return true;
        }

        UserRideOccurrenceDocument? current = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildOwnedAnyStateFilter(
                request.OccurrenceId.Value,
                request.VisitId.Value,
                request.UserId))
            .FirstOrDefaultAsync(cancellationToken);
        return current is not null
            && string.Equals(
                current.LastReorderOperationKeyHash,
                operationKeyHash,
                StringComparison.Ordinal);
    }
}
