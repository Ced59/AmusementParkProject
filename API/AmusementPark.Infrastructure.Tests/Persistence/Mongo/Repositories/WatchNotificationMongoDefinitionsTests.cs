using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class WatchNotificationMongoDefinitionsTests
{
    [Fact]
    public void BuildSubscriptionPublicationCutoff_ShouldExcludeLaterOptIns()
    {
        DateTime publishedAtUtc = new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        FilterDefinition<WatchSubscriptionDocument> filter =
            WatchNotificationMongoDefinitions.BuildSubscriptionPublicationCutoff(publishedAtUtc);
        IBsonSerializer<WatchSubscriptionDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<WatchSubscriptionDocument>();

        BsonDocument rendered = filter.Render(
            new RenderArgs<WatchSubscriptionDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));

        long expectedTimestamp = new DateTimeOffset(publishedAtUtc).ToUnixTimeMilliseconds();
        Assert.Equal(
            expectedTimestamp,
            rendered["createdAt"]["$lte"].AsBsonDateTime.MillisecondsSinceEpoch);
        Assert.Equal(
            expectedTimestamp,
            rendered["updatedAt"]["$lte"].AsBsonDateTime.MillisecondsSinceEpoch);
    }

    [Fact]
    public void BuildSubscriptionMutation_ShouldPreserveOwnerSlotAndImmutableIdentity()
    {
        DateTime createdAtUtc = new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-2"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            createdAtUtc);
        subscription.UpdatePreferences(
            new[] { FactualEventType.ParkNameChanged, FactualEventType.OperatorChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            createdAtUtc.AddMinutes(1));

        UpdateDefinition<WatchSubscriptionDocument> update =
            WatchNotificationMongoDefinitions.BuildSubscriptionMutation(subscription);
        IBsonSerializer<WatchSubscriptionDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<WatchSubscriptionDocument>();
        BsonDocument set = update.Render(
                new RenderArgs<WatchSubscriptionDocument>(
                    serializer,
                    BsonSerializer.SerializerRegistry))
            .AsBsonDocument["$set"]
            .AsBsonDocument;

        Assert.Equal(2, set["version"].AsInt64);
        Assert.Equal(2, set["eventTypes"].AsBsonArray.Count);
        Assert.DoesNotContain("ownerSlot", set.Names);
        Assert.DoesNotContain("userId", set.Names);
        Assert.DoesNotContain("targetType", set.Names);
        Assert.DoesNotContain("targetId", set.Names);
        Assert.DoesNotContain("createdAt", set.Names);
    }

    [Fact]
    public void BuildSubscriptionIndexes_ShouldProtectLogicalIdentityAndOwnerLimit()
    {
        IReadOnlyCollection<CreateIndexModel<WatchSubscriptionDocument>> indexes =
            WatchNotificationMongoDefinitions.BuildSubscriptionIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_watch_subscription_owner_target"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "uq_watch_subscription_owner_slot"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_watch_subscription_distribution"
            && index.Options.Unique != true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_watch_subscription_pilot_active_types");
    }

    [Fact]
    public void BuildNotificationIndexes_ShouldProtectIdempotencyFiltersAndRetention()
    {
        IReadOnlyCollection<CreateIndexModel<UserNotificationDocument>> indexes =
            WatchNotificationMongoDefinitions.BuildNotificationIndexes();
        CreateIndexModel<UserNotificationDocument> misleadingReports = indexes.Single(
            index => index.Options.Name == "ix_user_notification_pilot_misleading_reported");
        IBsonSerializer<UserNotificationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserNotificationDocument>();
        BsonDocument misleadingReportKeys = misleadingReports.Keys.Render(
            new RenderArgs<UserNotificationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));

        Assert.Contains(indexes, index => index.Options.Name == "uq_user_notification_event"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_user_notification_filters");
        Assert.Contains(indexes, index => index.Options.Name == "ix_user_notification_digest");
        Assert.Contains(indexes, index => index.Options.Name == "ix_user_notification_correction_distribution");
        Assert.Contains(indexes, index => index.Options.Name == "ttl_user_notification_retention"
            && index.Options.ExpireAfter == TimeSpan.Zero);
        Assert.Contains(indexes, index => index.Options.Name == "ix_user_notification_pilot_delivered");
        Assert.True(misleadingReports.Options.Sparse);
        Assert.Equal(1, misleadingReportKeys[UserNotificationDocument.MisleadingReportedAtFieldName]);
    }

    [Fact]
    public void BuildDigestNotificationFilter_ShouldApplySubscriptionSpecificEventTypesBeforeLimit()
    {
        DateTime periodStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
        FilterDefinition<UserNotificationDocument> filter =
            WatchNotificationMongoDefinitions.BuildDigestNotificationFilter(
                "user-1",
                periodStartUtc,
                periodStartUtc.AddDays(1),
                new[]
                {
                    new NotificationDigestSubscriptionFilter(
                        WatchSubscriptionId.Parse("subscription-1"),
                        new[] { FactualEventType.ParkNameChanged }),
                });
        IBsonSerializer<UserNotificationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserNotificationDocument>();

        BsonDocument rendered = filter.Render(
            new RenderArgs<UserNotificationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));

        Assert.Equal("user-1", rendered["userId"].AsString);
        BsonDocument subscriptionClause = Assert.Single(rendered["$or"].AsBsonArray).AsBsonDocument;
        Assert.Equal("subscription-1", subscriptionClause["subscriptionId"].AsString);
        Assert.Equal(
            (int)FactualEventType.ParkNameChanged,
            Assert.Single(subscriptionClause["eventType"].AsBsonDocument["$in"].AsBsonArray).AsInt32);
    }
}
