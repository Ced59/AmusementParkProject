using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class NotificationDigestRepository : INotificationDigestRepository
{
    private readonly IMongoCollection<NotificationDigestDocument> collection;

    public NotificationDigestRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal NotificationDigestRepository(IMongoCollection<NotificationDigestDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task ReplaceSnapshotAsync(
        NotificationDigest digest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(digest);
        NotificationDigestDocument document = digest.ToDocument();
        UpdateDefinition<NotificationDigestDocument> update =
            Builders<NotificationDigestDocument>.Update
                .SetOnInsert(static item => item.Id, document.Id)
                .SetOnInsert(static item => item.CreatedAt, document.CreatedAt)
                .Set(static item => item.UserId, document.UserId)
                .Set(static item => item.Channel, document.Channel)
                .Set(static item => item.Frequency, document.Frequency)
                .Set(static item => item.PeriodStart, document.PeriodStart)
                .Set(static item => item.PeriodEnd, document.PeriodEnd)
                .Set(static item => item.Entries, document.Entries)
                .Set(static item => item.ObservedNotificationCount, document.ObservedNotificationCount)
                .Set(static item => item.UpdatedAt, document.UpdatedAt);
        await this.collection.UpdateOneAsync(
            Builders<NotificationDigestDocument>.Filter.Eq(static item => item.Id, document.Id),
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<NotificationDigest?> GetAsync(
        NotificationDigestId digestId,
        CancellationToken cancellationToken)
    {
        NotificationDigestDocument? document = await this.collection.Find(
            Builders<NotificationDigestDocument>.Filter.Eq(
                static item => item.Id,
                digestId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task DeleteAsync(
        NotificationDigestId digestId,
        CancellationToken cancellationToken)
    {
        await this.collection.DeleteOneAsync(
            Builders<NotificationDigestDocument>.Filter.Eq(
                static item => item.Id,
                digestId.Value),
            cancellationToken);
    }

    private static IMongoCollection<NotificationDigestDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<NotificationDigestDocument>(
            settings.NotificationDigestsCollectionName);
    }
}
