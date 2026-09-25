using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripNotificationSubscriptionRepositoryTests
{
    [Fact]
    public void BuildIndexes_ShouldEnforceOneSubscriptionPerMemberAndTrip()
    {
        IReadOnlyCollection<CreateIndexModel<TripNotificationSubscriptionDocument>> indexes =
            TripNotificationSubscriptionRepository.BuildIndexes();

        CreateIndexModel<TripNotificationSubscriptionDocument> unique = Assert.Single(
            indexes,
            index => index.Options.Name == "uq_trip_notification_plan_user");
        Assert.True(unique.Options.Unique);
        Assert.Equal(
            new BsonDocument { { "tripPlanId", 1 }, { "userId", 1 } },
            unique.Keys.Render(new RenderArgs<TripNotificationSubscriptionDocument>(
                BsonSerializer.SerializerRegistry.GetSerializer<TripNotificationSubscriptionDocument>(),
                BsonSerializer.SerializerRegistry)));
    }
}
