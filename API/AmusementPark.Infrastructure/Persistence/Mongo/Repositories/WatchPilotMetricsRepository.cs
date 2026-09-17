using System.Globalization;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class WatchPilotMetricsRepository : IWatchPilotMetricsRepository
{
    private const int RetentionDays = 400;
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(10);
    internal static IReadOnlyCollection<string> MonitoredQueueKinds { get; } =
    [
        FactualNotificationDistributionJob.Kind,
        FactualNotificationCorrectionJob.Kind,
        NotificationDigestJob.Kind,
        NotificationEmailDeliveryJob.Kind,
    ];

    private readonly IMongoCollection<WatchPilotDailyMetricsDocument> dailyMetrics;
    private readonly IMongoCollection<WatchSubscriptionDocument> subscriptions;
    private readonly IMongoCollection<FactualChangeEventDocument> factualEvents;
    private readonly IMongoCollection<UserNotificationDocument> notifications;
    private readonly IMongoCollection<NotificationDigestDocument> digests;
    private readonly IMongoCollection<NotificationDeliveryAttemptDocument> deliveryAttempts;
    private readonly IMongoCollection<FactualChangeOutboxDocument> outbox;
    private readonly IMongoCollection<DurableBackgroundJobDocument> backgroundJobs;
    private readonly string factualEventsCollectionName;

    public WatchPilotMetricsRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.dailyMetrics = database.GetCollection<WatchPilotDailyMetricsDocument>(
            settings.WatchPilotDailyMetricsCollectionName);
        this.subscriptions = database.GetCollection<WatchSubscriptionDocument>(
            settings.WatchSubscriptionsCollectionName);
        this.factualEvents = database.GetCollection<FactualChangeEventDocument>(
            settings.FactualChangeEventsCollectionName);
        this.notifications = database.GetCollection<UserNotificationDocument>(
            settings.UserNotificationsCollectionName);
        this.digests = database.GetCollection<NotificationDigestDocument>(
            settings.NotificationDigestsCollectionName);
        this.deliveryAttempts = database.GetCollection<NotificationDeliveryAttemptDocument>(
            settings.NotificationDeliveryAttemptsCollectionName);
        this.outbox = database.GetCollection<FactualChangeOutboxDocument>(
            settings.FactualChangeOutboxCollectionName);
        this.backgroundJobs = database.GetCollection<DurableBackgroundJobDocument>(
            settings.DurableBackgroundJobsCollectionName);
        this.factualEventsCollectionName = settings.FactualChangeEventsCollectionName;
    }

    public async Task IncrementInteractionAsync(
        DateOnly dateUtc,
        WatchPilotInteractionKind interactionKind,
        long increment,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(interactionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(interactionKind));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(increment);

        DateTime dayUtc = dateUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        string dateKey = ToDateKey(dateUtc);
        UpdateDefinitionBuilder<WatchPilotDailyMetricsDocument> updates =
            Builders<WatchPilotDailyMetricsDocument>.Update;
        await this.dailyMetrics.UpdateOneAsync(
            Builders<WatchPilotDailyMetricsDocument>.Filter.Eq(
                static document => document.Id,
                dateKey),
            updates.Combine(
                updates.SetOnInsert(static document => document.Id, dateKey),
                updates.SetOnInsert(static document => document.DateUtc, dayUtc),
                updates.Set(static document => document.UpdatedAt, DateTime.UtcNow),
                updates.Set(static document => document.ExpiresAtUtc, dayUtc.AddDays(RetentionDays)),
                updates.Inc($"interactionCounts.{interactionKind}", increment)),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<WatchPilotMetricsSnapshot> ReadAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        DateTime normalizedFromUtc = StartOfUtcDay(EnsureUtc(fromUtc));
        DateTime normalizedToUtc = EndOfUtcDay(EnsureUtc(toUtc));
        long activeSubscriptions = await this.subscriptions.CountDocumentsAsync(
            Builders<WatchSubscriptionDocument>.Filter.Eq(
                static document => document.IsPaused,
                false),
            cancellationToken: cancellationToken);
        IReadOnlyDictionary<string, long> activeByType = await this.ReadActiveSubscriptionsAsync(
            cancellationToken);
        long verified = await this.factualEvents.CountDocumentsAsync(
            Builders<FactualChangeEventDocument>.Filter.Gte(
                static document => document.VerifiedAtUtc,
                normalizedFromUtc)
            & Builders<FactualChangeEventDocument>.Filter.Lte(
                static document => document.VerifiedAtUtc,
                normalizedToUtc),
            cancellationToken: cancellationToken);
        long published = await this.factualEvents.CountDocumentsAsync(
            Builders<FactualChangeEventDocument>.Filter.Gte(
                static document => document.PublishedAtUtc,
                normalizedFromUtc)
            & Builders<FactualChangeEventDocument>.Filter.Lte(
                static document => document.PublishedAtUtc,
                normalizedToUtc),
            cancellationToken: cancellationToken);
        long corrected = await this.CountTerminalEventsAsync(
            FactualChangeStatus.Corrected,
            normalizedFromUtc,
            normalizedToUtc,
            cancellationToken);
        long retracted = await this.CountTerminalEventsAsync(
            FactualChangeStatus.Retracted,
            normalizedFromUtc,
            normalizedToUtc,
            cancellationToken);
        FilterDefinition<UserNotificationDocument> notificationPeriod =
            Builders<UserNotificationDocument>.Filter.Gte(
                static document => document.DeliveredAt,
                normalizedFromUtc)
            & Builders<UserNotificationDocument>.Filter.Lte(
                static document => document.DeliveredAt,
                normalizedToUtc);
        long notificationCount = await this.notifications.CountDocumentsAsync(
            notificationPeriod,
            cancellationToken: cancellationToken);
        decimal averageLatencySeconds = await this.ReadAverageLatencySecondsAsync(
            normalizedFromUtc,
            normalizedToUtc,
            cancellationToken);
        long digestCount = await this.digests.CountDocumentsAsync(
            Builders<NotificationDigestDocument>.Filter.Gte(
                static document => document.CreatedAt,
                normalizedFromUtc)
            & Builders<NotificationDigestDocument>.Filter.Lte(
                static document => document.CreatedAt,
                normalizedToUtc),
            cancellationToken: cancellationToken);
        IReadOnlyDictionary<NotificationDeliveryAttemptStatus, long> deliveryCounts =
            await this.ReadDeliveryCountsAsync(normalizedFromUtc, normalizedToUtc, cancellationToken);
        long pendingOutbox = await this.outbox.CountDocumentsAsync(
            Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static document => document.MaterializedAtUtc,
                null)
            & Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static document => document.TerminalAtUtc,
                null),
            cancellationToken: cancellationToken);
        IReadOnlyDictionary<string, long> queueCounts = await this.ReadQueueCountsAsync(
            cancellationToken);
        IReadOnlyCollection<WatchPilotDailyMetrics> daily = await this.ReadDailyAsync(
            normalizedFromUtc,
            normalizedToUtc,
            cancellationToken);
        long duplicates = daily.Sum(day => day.InteractionCounts.GetValueOrDefault(
            WatchPilotInteractionKind.DuplicateDeliveryPrevented.ToString()));
        return new WatchPilotMetricsSnapshot(
            activeSubscriptions,
            activeByType,
            verified,
            published,
            corrected,
            retracted,
            notificationCount,
            duplicates,
            averageLatencySeconds,
            digestCount,
            deliveryCounts.GetValueOrDefault(NotificationDeliveryAttemptStatus.Pending),
            deliveryCounts.GetValueOrDefault(NotificationDeliveryAttemptStatus.Succeeded),
            deliveryCounts.GetValueOrDefault(NotificationDeliveryAttemptStatus.Failed),
            deliveryCounts.GetValueOrDefault(NotificationDeliveryAttemptStatus.Cancelled),
            false,
            null,
            null,
            pendingOutbox,
            queueCounts,
            daily);
    }

    private async Task<IReadOnlyDictionary<string, long>> ReadActiveSubscriptionsAsync(
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        [
            new BsonDocument("$match", new BsonDocument("isPaused", false)),
            new BsonDocument("$unwind", "$eventTypes"),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$eventTypes",
                ["count"] = new BsonDocument("$sum", 1),
            }),
        ];
        List<BsonDocument> documents = await this.subscriptions
            .Aggregate<BsonDocument>(pipeline, new AggregateOptions { MaxTime = QueryTimeout })
            .ToListAsync(cancellationToken);
        Dictionary<string, long> counts = new(StringComparer.Ordinal);
        foreach (BsonDocument document in documents)
        {
            if (!document.TryGetValue("_id", out BsonValue? value) || !value.IsNumeric)
            {
                continue;
            }

            FactualEventType type = (FactualEventType)value.ToInt32();
            if (Enum.IsDefined(type))
            {
                counts[type.ToString()] = ReadLong(document, "count");
            }
        }

        return counts;
    }

    private Task<long> CountTerminalEventsAsync(
        FactualChangeStatus status,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<FactualChangeEventDocument> filters =
            Builders<FactualChangeEventDocument>.Filter;
        return this.factualEvents.CountDocumentsAsync(
            filters.Eq(static document => document.Status, status)
            & filters.Gte(static document => document.TerminalAtUtc, fromUtc)
            & filters.Lte(static document => document.TerminalAtUtc, toUtc),
            cancellationToken: cancellationToken);
    }

    private async Task<decimal> ReadAverageLatencySecondsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        [
            PeriodMatch("deliveredAt", fromUtc, toUtc),
            new BsonDocument("$lookup", new BsonDocument
            {
                ["from"] = this.factualEventsCollectionName,
                ["localField"] = "factualEventId",
                ["foreignField"] = "_id",
                ["as"] = "factualEvent",
            }),
            new BsonDocument("$unwind", "$factualEvent"),
            new BsonDocument("$match", new BsonDocument("factualEvent.publishedAtUtc", new BsonDocument("$ne", BsonNull.Value))),
            new BsonDocument("$project", new BsonDocument("latencyMilliseconds", new BsonDocument("$max", new BsonArray
            {
                0,
                new BsonDocument("$subtract", new BsonArray { "$deliveredAt", "$factualEvent.publishedAtUtc" }),
            }))),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = BsonNull.Value,
                ["averageMilliseconds"] = new BsonDocument("$avg", "$latencyMilliseconds"),
            }),
        ];
        BsonDocument? document = await this.notifications
            .Aggregate<BsonDocument>(pipeline, new AggregateOptions { MaxTime = QueryTimeout })
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null
            || !document.TryGetValue("averageMilliseconds", out BsonValue? value)
            || !value.IsNumeric)
        {
            return 0m;
        }

        return Math.Round((decimal)value.ToDouble() / 1000m, 1, MidpointRounding.AwayFromZero);
    }

    private async Task<IReadOnlyDictionary<NotificationDeliveryAttemptStatus, long>> ReadDeliveryCountsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        [
            PeriodMatch("createdAt", fromUtc, toUtc),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$status",
                ["count"] = new BsonDocument("$sum", 1),
            }),
        ];
        List<BsonDocument> documents = await this.deliveryAttempts
            .Aggregate<BsonDocument>(pipeline, new AggregateOptions { MaxTime = QueryTimeout })
            .ToListAsync(cancellationToken);
        Dictionary<NotificationDeliveryAttemptStatus, long> result = new();
        foreach (BsonDocument document in documents)
        {
            if (document.TryGetValue("_id", out BsonValue? value)
                && value.IsNumeric
                && Enum.IsDefined((NotificationDeliveryAttemptStatus)value.ToInt32()))
            {
                result[(NotificationDeliveryAttemptStatus)value.ToInt32()] = ReadLong(document, "count");
            }
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<string, long>> ReadQueueCountsAsync(
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        [
            new BsonDocument("$match", new BsonDocument(
                "kind",
                new BsonDocument("$in", new BsonArray(MonitoredQueueKinds)))),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$status",
                ["count"] = new BsonDocument("$sum", 1),
            }),
        ];
        List<BsonDocument> documents = await this.backgroundJobs
            .Aggregate<BsonDocument>(pipeline, new AggregateOptions { MaxTime = QueryTimeout })
            .ToListAsync(cancellationToken);
        return documents
            .Where(static document => document.TryGetValue("_id", out BsonValue? value)
                && value.IsString)
            .ToDictionary(
                static document => document["_id"].AsString,
                static document => ReadLong(document, "count"),
                StringComparer.Ordinal);
    }

    private async Task<IReadOnlyCollection<WatchPilotDailyMetrics>> ReadDailyAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        DateTime fromDayUtc = DateTime.SpecifyKind(fromUtc.Date, DateTimeKind.Utc);
        DateTime toDayUtc = DateTime.SpecifyKind(toUtc.Date, DateTimeKind.Utc);
        FilterDefinition<WatchPilotDailyMetricsDocument> filter =
            Builders<WatchPilotDailyMetricsDocument>.Filter.Gte(
                static document => document.DateUtc,
                fromDayUtc)
            & Builders<WatchPilotDailyMetricsDocument>.Filter.Lte(
                static document => document.DateUtc,
                toDayUtc);
        List<WatchPilotDailyMetricsDocument> interactionDocuments = await this.dailyMetrics
            .Find(filter)
            .ToListAsync(cancellationToken);
        IReadOnlyDictionary<string, long> notificationsByDay = await this.ReadDailyCountsAsync(
            this.notifications,
            "deliveredAt",
            fromUtc,
            toUtc,
            cancellationToken);
        IReadOnlyDictionary<string, long> digestsByDay = await this.ReadDailyCountsAsync(
            this.digests,
            "createdAt",
            fromUtc,
            toUtc,
            cancellationToken);
        IReadOnlyDictionary<string, long> misleadingReportsByDay = await this.ReadDailyCountsAsync(
            this.notifications,
            UserNotificationDocument.MisleadingReportedAtFieldName,
            fromUtc,
            toUtc,
            cancellationToken);
        Dictionary<string, WatchPilotDailyMetricsDocument> interactionsByDay = interactionDocuments
            .ToDictionary(static document => document.Id, StringComparer.Ordinal);
        List<WatchPilotDailyMetrics> result = new();
        for (DateTime dayUtc = fromDayUtc; dayUtc <= toDayUtc; dayUtc = dayUtc.AddDays(1))
        {
            string date = ToDateKey(DateOnly.FromDateTime(dayUtc));
            interactionsByDay.TryGetValue(date, out WatchPilotDailyMetricsDocument? interaction);
            Dictionary<string, long> interactionCounts = interaction is null
                ? new Dictionary<string, long>(StringComparer.Ordinal)
                : new Dictionary<string, long>(interaction.InteractionCounts, StringComparer.Ordinal);
            interactionCounts[WatchPilotInteractionKind.MisleadingAlertReported.ToString()] =
                misleadingReportsByDay.GetValueOrDefault(date);
            result.Add(new WatchPilotDailyMetrics(
                date,
                notificationsByDay.GetValueOrDefault(date),
                digestsByDay.GetValueOrDefault(date),
                interactionCounts));
            if (dayUtc == toDayUtc)
            {
                break;
            }
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<string, long>> ReadDailyCountsAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        string dateField,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        [
            PeriodMatch(dateField, fromUtc, toUtc),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument("$dateToString", new BsonDocument
                {
                    ["format"] = "%Y-%m-%d",
                    ["date"] = $"${dateField}",
                    ["timezone"] = "UTC",
                }),
                ["count"] = new BsonDocument("$sum", 1),
            }),
        ];
        List<BsonDocument> documents = await collection
            .Aggregate<BsonDocument>(pipeline, new AggregateOptions { MaxTime = QueryTimeout })
            .ToListAsync(cancellationToken);
        return documents.ToDictionary(
            static document => document["_id"].AsString,
            static document => ReadLong(document, "count"),
            StringComparer.Ordinal);
    }

    private static BsonDocument PeriodMatch(string field, DateTime fromUtc, DateTime toUtc)
    {
        return new BsonDocument("$match", new BsonDocument(field, new BsonDocument
        {
            ["$gte"] = fromUtc,
            ["$lte"] = toUtc,
        }));
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }

    private static DateTime StartOfUtcDay(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    private static DateTime EndOfUtcDay(DateTime value)
    {
        return StartOfUtcDay(value).AddDays(1).AddTicks(-1);
    }

    private static long ReadLong(BsonDocument document, string field)
    {
        return document.TryGetValue(field, out BsonValue? value) && value.IsNumeric
            ? value.ToInt64()
            : 0L;
    }

    private static string ToDateKey(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
