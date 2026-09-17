using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
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
    private readonly IMongoCollection<BsonDocument> parks;
    private readonly IMongoCollection<BsonDocument> parkItems;

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
        this.parks = database.GetCollection<BsonDocument>(settings.ParksCollectionName);
        this.parkItems = database.GetCollection<BsonDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<PassportWatchlistTargetCatalog> LoadTargetsAsync(
        IReadOnlyCollection<string> parkIds,
        IReadOnlyCollection<string> parkItemIds,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        ArgumentNullException.ThrowIfNull(sourceBudget);
        string[] normalizedParkIds = NormalizeIds(parkIds);
        string[] normalizedParkItemIds = NormalizeIds(parkItemIds);
        BsonDocument[] itemDocuments = await LoadTargetDocumentsAsync(
            this.parkItems,
            normalizedParkItemIds,
            new BsonDocument
            {
                ["_id"] = 1,
                ["parkId"] = 1,
                ["name"] = 1,
                ["category"] = 1,
                ["isVisible"] = 1,
                ["attractionDetails.status"] = 1,
            },
            sourceBudget,
            cancellationToken);
        string[] parentParkIds = itemDocuments
            .Select(static document => ReadString(document, "parkId"))
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Concat(normalizedParkIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        BsonDocument[] parkDocuments = await LoadTargetDocumentsAsync(
            this.parks,
            parentParkIds,
            new BsonDocument
            {
                ["_id"] = 1,
                ["name"] = 1,
                ["status"] = 1,
                ["isVisible"] = 1,
                ["adminReviewStatus"] = 1,
            },
            sourceBudget,
            cancellationToken);
        return MapTargets(
            normalizedParkIds,
            normalizedParkItemIds,
            parkDocuments,
            itemDocuments);
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

    internal static PassportWatchlistTargetCatalog MapTargets(
        IReadOnlyCollection<string> parkIds,
        IReadOnlyCollection<string> parkItemIds,
        IReadOnlyCollection<BsonDocument> parkDocuments,
        IReadOnlyCollection<BsonDocument> itemDocuments)
    {
        Dictionary<string, Park> publicParks = parkDocuments
            .Select(MapPark)
            .Where(static park => park is not null && park.IsPubliclyDiscoverable())
            .Cast<Park>()
            .ToDictionary(static park => park.Id, StringComparer.Ordinal);
        Dictionary<string, PassportWatchlistTargetSnapshot> parkTargets = parkIds
            .ToDictionary(
                static id => id,
                static _ => Unavailable(CollectionTargetType.Park),
                StringComparer.Ordinal);
        foreach (string parkId in parkIds)
        {
            if (!publicParks.TryGetValue(parkId, out Park? park))
            {
                continue;
            }

            parkTargets[parkId] = new PassportWatchlistTargetSnapshot(
                CollectionTargetType.Park,
                CollectionTargetStatusResolver.Resolve(park.Status),
                park.Name,
                null);
        }

        Dictionary<string, PassportWatchlistTargetSnapshot> parkItemTargets = parkItemIds
            .ToDictionary(
                static id => id,
                static _ => Unavailable(CollectionTargetType.ParkItem),
                StringComparer.Ordinal);
        foreach (BsonDocument document in itemDocuments)
        {
            ParkItem? item = MapParkItem(document);
            if (item is null
                || !parkItemTargets.ContainsKey(item.Id)
                || !item.IsVisible
                || !publicParks.TryGetValue(item.ParkId, out Park? parentPark))
            {
                continue;
            }

            parkItemTargets[item.Id] = new PassportWatchlistTargetSnapshot(
                CollectionTargetType.ParkItem,
                CollectionTargetStatusResolver.Resolve(item, parentPark.Status),
                item.Name,
                parentPark.Name);
        }

        return new PassportWatchlistTargetCatalog(parkTargets, parkItemTargets);
    }

    private static async Task<BsonDocument[]> LoadTargetDocumentsAsync(
        IMongoCollection<BsonDocument> collection,
        IReadOnlyCollection<string> ids,
        ProjectionDefinition<BsonDocument> projection,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<BsonDocument>();
        }

        List<BsonDocument> result = new List<BsonDocument>();
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.In("_id", ids);
        using IAsyncCursor<BsonDocument> cursor = await collection
            .Find(filter)
            .Project<BsonDocument>(projection)
            .Sort(new BsonDocument("_id", 1))
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (BsonDocument document in cursor.Current)
            {
                Consume(sourceBudget, document);
                result.Add(document);
            }
        }

        return result.ToArray();
    }

    private static Park? MapPark(BsonDocument document)
    {
        string? id = ReadString(document, "_id");
        string? name = ReadString(document, "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new Park
        {
            Id = id,
            Name = name,
            Status = ReadEnum(document, "status", ParkStatus.Operating),
            IsVisible = ReadBoolean(document, "isVisible"),
            AdminReviewStatus = ReadEnum(
                document,
                "adminReviewStatus",
                AdminReviewStatus.ToReview),
        };
    }

    private static ParkItem? MapParkItem(BsonDocument document)
    {
        string? id = ReadString(document, "_id");
        string? parkId = ReadString(document, "parkId");
        string? name = ReadString(document, "name");
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(parkId)
            || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        string? attractionStatus = document.TryGetValue(
                "attractionDetails",
                out BsonValue? attractionDetailsValue)
            && attractionDetailsValue.IsBsonDocument
                ? ReadString(attractionDetailsValue.AsBsonDocument, "status")
                : null;
        return new ParkItem
        {
            Id = id,
            ParkId = parkId,
            Name = name,
            Category = ReadEnum(document, "category", ParkItemCategory.Other),
            IsVisible = ReadBoolean(document, "isVisible"),
            AttractionDetails = attractionStatus is null
                ? null
                : new AttractionDetails { Status = attractionStatus },
        };
    }

    private static string[] NormalizeIds(IReadOnlyCollection<string> ids)
    {
        return ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string? ReadString(BsonDocument document, string elementName)
    {
        return document.TryGetValue(elementName, out BsonValue? value) && value.IsString
            ? value.AsString
            : null;
    }

    private static bool ReadBoolean(BsonDocument document, string elementName)
    {
        return document.TryGetValue(elementName, out BsonValue? value)
            && value.IsBoolean
            && value.AsBoolean;
    }

    private static TEnum ReadEnum<TEnum>(
        BsonDocument document,
        string elementName,
        TEnum fallback)
        where TEnum : struct, Enum
    {
        if (!document.TryGetValue(elementName, out BsonValue? value))
        {
            return fallback;
        }

        if (value.IsString && Enum.TryParse(value.AsString, true, out TEnum parsed))
        {
            return parsed;
        }

        if (value.IsInt32 && Enum.IsDefined(typeof(TEnum), value.AsInt32))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), value.AsInt32);
        }

        return fallback;
    }

    private static PassportWatchlistTargetSnapshot Unavailable(CollectionTargetType targetType)
    {
        return new PassportWatchlistTargetSnapshot(
            targetType,
            CollectionTargetStatus.Unknown,
            null,
            null);
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
