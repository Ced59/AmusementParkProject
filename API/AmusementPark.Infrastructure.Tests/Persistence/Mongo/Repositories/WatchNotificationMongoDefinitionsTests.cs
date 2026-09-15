using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class WatchNotificationMongoDefinitionsTests
{
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
