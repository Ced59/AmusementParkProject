using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class MongoWatchlistExportStore : IWatchlistExportStore
{
    private readonly IMongoCollection<UserCollectionEntryDocument> collectionEntries;
    private readonly IMongoCollection<WatchSubscriptionDocument> subscriptions;
    private readonly IMongoCollection<UserNotificationDocument> notifications;
    private readonly IMongoCollection<NotificationDigestDocument> digests;
    private readonly IMongoCollection<NotificationEmailPreferenceDocument> emailPreferences;
    private readonly IMongoCollection<NotificationDeliveryAttemptDocument> deliveryAttempts;
    private readonly IMongoCollection<FactualChangeEventDocument> factualEvents;

    public MongoWatchlistExportStore(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collectionEntries = database.GetCollection<UserCollectionEntryDocument>(
            settings.UserCollectionEntriesCollectionName);
        this.subscriptions = database.GetCollection<WatchSubscriptionDocument>(
            settings.WatchSubscriptionsCollectionName);
        this.notifications = database.GetCollection<UserNotificationDocument>(
            settings.UserNotificationsCollectionName);
        this.digests = database.GetCollection<NotificationDigestDocument>(
            settings.NotificationDigestsCollectionName);
        this.emailPreferences = database.GetCollection<NotificationEmailPreferenceDocument>(
            settings.NotificationPreferencesCollectionName);
        this.deliveryAttempts = database.GetCollection<NotificationDeliveryAttemptDocument>(
            settings.NotificationDeliveryAttemptsCollectionName);
        this.factualEvents = database.GetCollection<FactualChangeEventDocument>(
            settings.FactualChangeEventsCollectionName);
    }

    public async Task<IReadOnlyCollection<FactualChangeEvent>> LoadFactualEventsAsync(
        IReadOnlyCollection<FactualChangeEventId> eventIds,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventIds);
        ArgumentNullException.ThrowIfNull(sourceBudget);
        if (eventIds.Count == 0)
        {
            return Array.Empty<FactualChangeEvent>();
        }

        string[] values = eventIds
            .Select(static eventId => eventId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return await LoadAsync(
            this.factualEvents
                .Find(Builders<FactualChangeEventDocument>.Filter.In(
                    static document => document.Id,
                    values))
                .SortBy(static document => document.Id),
            sourceBudget,
            static document => document.ToDomain(),
            cancellationToken);
    }

    public async Task<PassportWatchlistStoredExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        UserCollectionEntry[] collectionEntries = await LoadAsync(
            this.collectionEntries
                .Find(document => document.UserId == userId)
                .SortBy(static document => document.CreatedAt)
                .ThenBy(static document => document.Id),
            sourceBudget,
            static document => document.ToDomain(),
            cancellationToken);
        WatchSubscription[] loadedSubscriptions = await LoadAsync(
            this.subscriptions
                .Find(document => document.UserId == userId)
                .SortBy(static document => document.CreatedAt)
                .ThenBy(static document => document.Id),
            sourceBudget,
            static document => document.ToDomain(),
            cancellationToken);
        UserNotification[] loadedNotifications = await LoadAsync(
            this.notifications
                .Find(document => document.UserId == userId)
                .SortBy(static document => document.DeliveredAt)
                .ThenBy(static document => document.Id),
            sourceBudget,
            static document => document.ToDomain(),
            cancellationToken);
        NotificationDigest[] loadedDigests = await LoadAsync(
            this.digests
                .Find(document => document.UserId == userId)
                .SortBy(static document => document.PeriodStart)
                .ThenBy(static document => document.Id),
            sourceBudget,
            static document => document.ToDomain(),
            cancellationToken);

        NotificationEmailPreferenceDocument? preferenceDocument = await this.emailPreferences
            .Find(document => document.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        if (preferenceDocument is not null)
        {
            Consume(sourceBudget, preferenceDocument);
        }

        NotificationDeliveryAttempt[] loadedAttempts = await LoadAsync(
            this.deliveryAttempts
                .Find(document => document.UserId == userId)
                .SortBy(static document => document.CreatedAt)
                .ThenBy(static document => document.Id),
            sourceBudget,
            static document => document.ToDomain(),
            cancellationToken);

        return new PassportWatchlistStoredExportData(
            collectionEntries,
            loadedSubscriptions,
            loadedNotifications,
            loadedDigests,
            preferenceDocument?.ToDomain(),
            loadedAttempts);
    }

    private static async Task<TDomain[]> LoadAsync<TDocument, TDomain>(
        IFindFluent<TDocument, TDocument> query,
        PassportExportSourceBudget sourceBudget,
        Func<TDocument, TDomain> map,
        CancellationToken cancellationToken)
        where TDocument : class
    {
        List<TDomain> result = new List<TDomain>();
        using IAsyncCursor<TDocument> cursor = await query.ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (TDocument document in cursor.Current)
            {
                Consume(sourceBudget, document);
                result.Add(map(document));
            }
        }

        return result.ToArray();
    }

    private static void Consume<TDocument>(
        PassportExportSourceBudget sourceBudget,
        TDocument document)
        where TDocument : class
    {
        if (!sourceBudget.TryConsume(document.ToBson().LongLength))
        {
            throw new PassportExportSizeLimitException();
        }
    }
}
