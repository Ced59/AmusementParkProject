using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class NotificationDeliveryAttemptMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldPreventDuplicateDeliveryAndExpireOperationalData()
    {
        IReadOnlyCollection<CreateIndexModel<NotificationDeliveryAttemptDocument>> indexes =
            NotificationDeliveryAttemptMongoDefinitions.BuildIndexes();

        CreateIndexModel<NotificationDeliveryAttemptDocument> unique = indexes.Single(
            index => index.Options.Name == "uq_notification_delivery_digest");
        CreateIndexModel<NotificationDeliveryAttemptDocument> expiry = indexes.Single(
            index => index.Options.Name == "ttl_notification_delivery_expiry");
        Assert.True(unique.Options.Unique);
        Assert.Equal(TimeSpan.Zero, expiry.Options.ExpireAfter);
    }
}
