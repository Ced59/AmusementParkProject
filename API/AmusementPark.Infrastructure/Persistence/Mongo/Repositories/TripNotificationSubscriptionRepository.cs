using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripNotificationSubscriptionRepository
    : ITripNotificationSubscriptionRepository
{
    private readonly IMongoCollection<TripNotificationSubscriptionDocument> collection;

    public TripNotificationSubscriptionRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripNotificationSubscriptionDocument>(
            settings.TripNotificationSubscriptionsCollectionName);
    }

    internal TripNotificationSubscriptionRepository(
        IMongoCollection<TripNotificationSubscriptionDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<TripNotificationSubscription?> GetAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken)
    {
        TripNotificationSubscriptionDocument? document = await this.collection.Find(
                Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                    static item => item.TripPlanId,
                    tripPlanId.Value)
                & Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                    static item => item.UserId,
                    userId))
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<bool> CreateAsync(
        TripNotificationSubscription subscription,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        try
        {
            await this.collection.InsertOneAsync(
                ToDocument(subscription),
                cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task<bool> ReplaceAsync(
        TripNotificationSubscription subscription,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.Id,
                subscription.Id)
            & Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.TripPlanId,
                subscription.TripPlanId.Value)
            & Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.UserId,
                subscription.UserId)
            & Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.Version,
                expectedVersion),
            ToDocument(subscription),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task DeleteForMemberAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken)
    {
        _ = await this.collection.DeleteOneAsync(
            Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.TripPlanId,
                tripPlanId.Value)
            & Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.UserId,
                userId),
            cancellationToken);
    }

    public async Task DeleteForTripAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        _ = await this.collection.DeleteManyAsync(
            Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.TripPlanId,
                tripPlanId.Value),
            cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<TripNotificationSubscriptionDocument>>
        BuildIndexes()
    {
        IndexKeysDefinitionBuilder<TripNotificationSubscriptionDocument> keys =
            Builders<TripNotificationSubscriptionDocument>.IndexKeys;
        return new[]
        {
            new CreateIndexModel<TripNotificationSubscriptionDocument>(
                keys.Ascending(static item => item.TripPlanId)
                    .Ascending(static item => item.UserId),
                new CreateIndexOptions
                {
                    Name = "uq_trip_notification_plan_user",
                    Unique = true,
                }),
            new CreateIndexModel<TripNotificationSubscriptionDocument>(
                keys.Ascending(static item => item.UserId)
                    .Ascending(static item => item.IsEnabled),
                new CreateIndexOptions { Name = "ix_trip_notification_user_enabled" }),
        };
    }

    private static TripNotificationSubscription ToDomain(
        TripNotificationSubscriptionDocument document)
    {
        return TripNotificationSubscription.Restore(
            document.Id,
            TripPlanId.Parse(document.TripPlanId),
            document.UserId,
            document.IsEnabled,
            document.SeenThroughSequence,
            DateTime.SpecifyKind(document.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(document.UpdatedAt, DateTimeKind.Utc),
            document.Version);
    }

    private static TripNotificationSubscriptionDocument ToDocument(
        TripNotificationSubscription subscription)
    {
        return new TripNotificationSubscriptionDocument
        {
            Id = subscription.Id,
            TripPlanId = subscription.TripPlanId.Value,
            UserId = subscription.UserId,
            IsEnabled = subscription.IsEnabled,
            SeenThroughSequence = subscription.SeenThroughSequence,
            CreatedAt = subscription.CreatedAtUtc,
            UpdatedAt = subscription.UpdatedAtUtc,
            Version = subscription.Version,
        };
    }
}
