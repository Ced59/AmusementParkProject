using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class NotificationDigestTests
{
    private static readonly DateTime MondayUtc =
        new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(NotificationFrequency.DailyDigest, 2026, 9, 17)]
    [InlineData(NotificationFrequency.WeeklyDigest, 2026, 9, 14)]
    public void ResolveStart_ShouldAlignUtcPeriod(
        NotificationFrequency frequency,
        int year,
        int month,
        int day)
    {
        DateTime timestamp = new DateTime(2026, 9, 17, 18, 30, 0, DateTimeKind.Utc);

        DateTime result = NotificationDigestPeriodResolver.ResolveStart(frequency, timestamp);

        Assert.Equal(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void CreateSnapshot_ShouldKeepOnlyLatestLogicalRevisionDeterministically()
    {
        NotificationDigestEntry first = CreateEntry("event-a", 1, MondayUtc.AddHours(8));
        NotificationDigestEntry corrected = CreateEntry("event-b", 2, MondayUtc.AddHours(9));

        NotificationDigest digest = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.WeeklyDigest,
            MondayUtc,
            new[] { corrected, first },
            observedNotificationCount: 2,
            MondayUtc.AddHours(10));

        NotificationDigestEntry entry = Assert.Single(digest.Entries);
        Assert.Equal("event-b", entry.FactualEventId.Value);
        Assert.Equal(2, entry.SourceRevision);
        Assert.Equal(2, digest.ObservedNotificationCount);
        Assert.Equal(
            NotificationDigestId.ForGroup(
                "user-1",
                NotificationChannel.Email,
                NotificationFrequency.WeeklyDigest,
                MondayUtc),
            digest.Id);
    }

    [Fact]
    public void CreateSnapshot_WithSameInputs_ShouldKeepSameIdentityAndEntryOrder()
    {
        NotificationDigestEntry first = CreateEntry("event-a", 1, MondayUtc.AddHours(8), "logical-b");
        NotificationDigestEntry second = CreateEntry("event-b", 1, MondayUtc.AddHours(9), "logical-a");

        NotificationDigest left = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            MondayUtc,
            new[] { first, second },
            2,
            MondayUtc.AddHours(10));
        NotificationDigest right = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            MondayUtc,
            new[] { second, first },
            2,
            MondayUtc.AddHours(11));

        Assert.Equal(left.Id, right.Id);
        Assert.Equal(
            left.Entries.Select(static entry => entry.DeduplicationKey),
            right.Entries.Select(static entry => entry.DeduplicationKey));
    }

    private static NotificationDigestEntry CreateEntry(
        string eventId,
        long revision,
        DateTime occurredAtUtc,
        string deduplicationKey = "logical-change")
    {
        return new NotificationDigestEntry(
            FactualChangeEventId.Parse(eventId),
            WatchSubscriptionId.Parse("subscription-1"),
            deduplicationKey,
            revision,
            FactualEventType.OpeningDateConfirmed,
            FactualTargetType.ParkItem,
            "item-1",
            FactualChangeStatus.Published,
            occurredAtUtc);
    }
}
