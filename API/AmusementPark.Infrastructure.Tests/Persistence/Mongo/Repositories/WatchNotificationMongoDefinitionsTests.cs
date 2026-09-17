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
    }

    [Fact]
    public void BuildNotificationIndexes_ShouldProtectIdempotencyFiltersAndRetention()
    {
        IReadOnlyCollection<CreateIndexModel<UserNotificationDocument>> indexes =
            WatchNotificationMongoDefinitions.BuildNotificationIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_user_notification_event"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_user_notification_filters");
        Assert.Contains(indexes, index => index.Options.Name == "ttl_user_notification_retention"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }
}
