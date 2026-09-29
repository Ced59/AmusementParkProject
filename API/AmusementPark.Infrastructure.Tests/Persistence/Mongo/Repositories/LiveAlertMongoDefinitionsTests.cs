using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LiveAlertMongoDefinitionsTests
{
    [Fact]
    public void BuildSubscriptionIndexes_ShouldProtectIdentityAndAtomicUserQuota()
    {
        IReadOnlyCollection<CreateIndexModel<LiveAlertSubscriptionDocument>> indexes =
            LiveAlertMongoDefinitions.BuildSubscriptionIndexes();

        CreateIndexModel<LiveAlertSubscriptionDocument> quota = Assert.Single(
            indexes,
            static index => index.Options.Name == "uq_live_alert_subscription_quota_slot");
        Assert.True(quota.Options.Unique);
        BsonDocument keys = quota.Keys.Render(
            new RenderArgs<LiveAlertSubscriptionDocument>(
                BsonSerializer.LookupSerializer<LiveAlertSubscriptionDocument>(),
                BsonSerializer.SerializerRegistry));
        Assert.Equal(1, keys["userId"].AsInt32);
        Assert.Equal(1, keys["quotaSlot"].AsInt32);
        Assert.Contains(
            indexes,
            static index => index.Options.Name == "ix_live_alert_subscription_pending_delivery");
        CreateIndexModel<LiveAlertSubscriptionDocument> retention = Assert.Single(
            indexes,
            static index => index.Options.Name == "ttl_live_alert_subscription");
        BsonDocument retentionKeys = retention.Keys.Render(
            new RenderArgs<LiveAlertSubscriptionDocument>(
                BsonSerializer.LookupSerializer<LiveAlertSubscriptionDocument>(),
                BsonSerializer.SerializerRegistry));
        Assert.Equal(1, retentionKeys["retentionExpiresAt"].AsInt32);
    }

    [Fact]
    public void BuildPendingDeliveryFilter_ShouldIgnoreBusinessExpiration()
    {
        DateTime nowUtc = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

        FilterDefinition<LiveAlertSubscriptionDocument> filter =
            LiveAlertMongoDefinitions.BuildPendingDeliveryFilter(nowUtc);
        BsonDocument rendered = filter.Render(
            new RenderArgs<LiveAlertSubscriptionDocument>(
                BsonSerializer.LookupSerializer<LiveAlertSubscriptionDocument>(),
                BsonSerializer.SerializerRegistry));

        Assert.True(rendered.Contains("pendingTrigger"));
        Assert.True(rendered.Contains("retentionExpiresAt"));
        Assert.False(rendered.Contains("expiresAt"));
    }
}
