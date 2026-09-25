using AmusementPark.Core.Domain.Trips;
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
    public void DocumentRoundTrip_ShouldPreserveTheMonotonicNotificationBoundary()
    {
        DateTime createdAtUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        DateTime updatedAtUtc = createdAtUtc.AddMilliseconds(1);
        TripNotificationSubscription subscription = TripNotificationSubscription.Restore(
            "subscription-1",
            TripPlanId.Parse("trip-1"),
            TripMemberId.Parse("membership-1"),
            "user-1",
            true,
            12,
            new[] { "pending-before-boundary" },
            createdAtUtc,
            updatedAtUtc,
            4);

        TripNotificationSubscriptionDocument document =
            TripNotificationSubscriptionRepository.ToDocument(subscription);
        TripNotificationSubscriptionDocument roundTripDocument =
            BsonSerializer.Deserialize<TripNotificationSubscriptionDocument>(
                document.ToBsonDocument());
        TripNotificationSubscription roundTrip =
            TripNotificationSubscriptionRepository.ToDomain(roundTripDocument);

        Assert.Equal(new[] { "pending-before-boundary" }, roundTripDocument.PendingOperationKeys);
        Assert.Equal(new[] { "pending-before-boundary" }, roundTrip.PendingOperationKeys);
        Assert.Equal(updatedAtUtc, roundTrip.UpdatedAtUtc);
    }

    [Fact]
    public void BuildExactSubscriptionFilter_ShouldFenceAConcurrentReplacement()
    {
        DateTime nowUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        TripNotificationSubscription subscription = TripNotificationSubscription.Restore(
            "subscription-1",
            TripPlanId.Parse("trip-1"),
            TripMemberId.Parse("membership-1"),
            "user-1",
            true,
            12,
            Array.Empty<string>(),
            nowUtc,
            nowUtc,
            4);

        FilterDefinition<TripNotificationSubscriptionDocument> filter =
            TripNotificationSubscriptionRepository.BuildExactSubscriptionFilter(subscription);

        BsonDocument rendered = filter.Render(
            new RenderArgs<TripNotificationSubscriptionDocument>(
                BsonSerializer.SerializerRegistry.GetSerializer<TripNotificationSubscriptionDocument>(),
                BsonSerializer.SerializerRegistry));
        Assert.Equal("subscription-1", rendered["_id"].AsString);
        Assert.Equal("trip-1", rendered["tripPlanId"].AsString);
        Assert.Equal("membership-1", rendered["memberId"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(4, rendered["version"].AsInt64);
    }

    [Fact]
    public void BuildCleanupPageFilter_ShouldContinueAfterOpaqueIdentifier()
    {
        FilterDefinition<TripNotificationSubscriptionDocument> filter =
            TripNotificationSubscriptionRepository.BuildCleanupPageFilter(" subscription-1 ");

        BsonDocument rendered = filter.Render(
            new RenderArgs<TripNotificationSubscriptionDocument>(
                BsonSerializer.SerializerRegistry.GetSerializer<TripNotificationSubscriptionDocument>(),
                BsonSerializer.SerializerRegistry));

        Assert.Equal(
            new BsonDocument("_id", new BsonDocument("$gt", "subscription-1")),
            rendered);
    }

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
