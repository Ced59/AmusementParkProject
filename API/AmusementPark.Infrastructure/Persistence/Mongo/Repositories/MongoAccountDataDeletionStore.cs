using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoAccountDataDeletionStore : IAccountDataDeletionStore
{
    private readonly MongoAccountPassportDataDeletion passportDeletion;
    private readonly MongoAccountTripDataDeletion tripDeletion;
    private readonly MongoAccountOperationalDataDeletion operationalDeletion;

    public MongoAccountDataDeletionStore(
        MongoAccountPassportDataDeletion passportDeletion,
        MongoAccountTripDataDeletion tripDeletion,
        MongoAccountOperationalDataDeletion operationalDeletion)
    {
        this.passportDeletion = passportDeletion;
        this.tripDeletion = tripDeletion;
        this.operationalDeletion = operationalDeletion;
    }

    public async Task<AccountDataDeletionResult> PurgeAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        long deletedCount = 0;
        deletedCount += await this.tripDeletion.PurgeAsync(userId, cancellationToken);
        deletedCount += await this.passportDeletion.PurgeAsync(userId, cancellationToken);
        deletedCount += await this.operationalDeletion.PurgeAsync(userId, cancellationToken);
        return new AccountDataDeletionResult(deletedCount);
    }
}
