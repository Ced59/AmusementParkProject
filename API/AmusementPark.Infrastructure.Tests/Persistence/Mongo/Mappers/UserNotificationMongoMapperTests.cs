using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class UserNotificationMongoMapperTests
{
    [Fact]
    public void RoundTrip_PreservesAnIdempotentMisleadingReport()
    {
        DateTime deliveredAtUtc = new(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
        DateTime reportedAtUtc = deliveredAtUtc.AddMinutes(5);
        UserNotification notification = UserNotification.Restore(
            UserNotificationId.Parse("notification-1"),
            "user-1",
            FactualChangeEventId.Parse("event-1"),
            WatchSubscriptionId.Parse("subscription-1"),
            FactualEventType.ParkNameChanged,
            FactualTargetType.Park,
            "park-1",
            "park-1",
            1,
            UserNotification.CurrentTemplateVersion,
            "FR",
            UserNotificationStatus.Delivered,
            deliveredAtUtc,
            deliveredAtUtc,
            null,
            null,
            deliveredAtUtc.AddDays(UserNotification.RetentionDays),
            2,
            reportedAtUtc);

        UserNotificationDocument document = notification.ToDocument();
        UserNotification restored = document.ToDomain();

        Assert.Equal(reportedAtUtc, document.MisleadingReportedAt);
        Assert.Equal(reportedAtUtc, document.UpdatedAt);
        Assert.Equal(reportedAtUtc, restored.MisleadingReportedAtUtc);
        Assert.Equal(2, restored.Version);
    }
}
