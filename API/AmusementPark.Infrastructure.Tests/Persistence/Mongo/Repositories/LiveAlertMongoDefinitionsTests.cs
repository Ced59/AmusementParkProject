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
    }
}
