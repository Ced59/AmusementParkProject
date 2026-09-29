using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LiveAlertNotificationRepository : ILiveAlertNotificationRepository
{
    private static readonly TimeSpan ActivityLeaseDuration = TimeSpan.FromMinutes(3);
    private readonly IMongoCollection<LiveAlertNotificationDocument> collection;
    private readonly IWatchlistAccountDeletionFence deletionFence;

    public LiveAlertNotificationRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IWatchlistAccountDeletionFence deletionFence)
    {
        this.collection = database.GetCollection<LiveAlertNotificationDocument>(
            settings.LiveAlertNotificationsCollectionName);
        this.deletionFence = deletionFence;
    }

    public async Task<IReadOnlyCollection<LiveAlertNotification>> ListOwnedAsync(
        string userId,
        DateTime nowUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        List<LiveAlertNotificationDocument> documents = await this.collection.Find(
            document => document.UserId == normalizedUserId
                && document.ExpiresAt > nowUtc
                && document.Status != UserNotificationStatus.Dismissed)
            .SortByDescending(static document => document.DeliveredAt)
            .ThenBy(static document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<int> CountUnreadAsync(
        string userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        long count = await this.collection.CountDocumentsAsync(
            document => document.UserId == normalizedUserId
                && document.ExpiresAt > nowUtc
                && document.Status == UserNotificationStatus.Delivered,
            cancellationToken: cancellationToken);
        return checked((int)count);
    }

    public async Task<LiveAlertNotification?> GetOwnedAsync(
        string userId,
        LiveAlertNotificationId notificationId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        LiveAlertNotificationDocument? document = await this.collection.Find(
            item => item.Id == notificationId.Value && item.UserId == normalizedUserId)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserNotificationWriteOutcome> CreateAsync(
        LiveAlertNotification notification,
        CancellationToken cancellationToken)
    {
        string? leaseId = await this.deletionFence.TryAcquireActivityLeaseAsync(
            notification.UserId,
            ActivityLeaseDuration,
            cancellationToken);
        if (leaseId is null)
        {
            return UserNotificationWriteOutcome.Conflict;
        }

        try
        {
            try
            {
                await this.collection.InsertOneAsync(
                    notification.ToDocument(),
                    cancellationToken: cancellationToken);
                return UserNotificationWriteOutcome.Success;
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return UserNotificationWriteOutcome.AlreadyExists;
            }
        }
        finally
        {
            await this.deletionFence.ReleaseActivityLeaseAsync(leaseId, CancellationToken.None);
        }
    }

    public async Task<UserNotificationWriteOutcome> ReplaceAsync(
        LiveAlertNotification notification,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        LiveAlertNotificationDocument document = notification.ToDocument();
        UpdateResult result = await this.collection.UpdateOneAsync(
            item => item.Id == document.Id
                && item.UserId == document.UserId
                && item.Version == expectedVersion,
            Builders<LiveAlertNotificationDocument>.Update
                .Set(static item => item.Status, document.Status)
                .Set(static item => item.ReadAt, document.ReadAt)
                .Set(static item => item.DismissedAt, document.DismissedAt)
                .Set(static item => item.UpdatedAt, document.UpdatedAt)
                .Set(static item => item.Version, document.Version),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1
            ? UserNotificationWriteOutcome.Success
            : UserNotificationWriteOutcome.Conflict;
    }
}
