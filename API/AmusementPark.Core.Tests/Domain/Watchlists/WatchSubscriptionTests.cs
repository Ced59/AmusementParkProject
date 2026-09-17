using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class WatchSubscriptionTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldCaptureAnExplicitActivePreference()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            " user-1 ",
            CollectionTargetType.Park,
            " park-1 ",
            new[]
            {
                FactualEventType.OpeningCalendarChanged,
                FactualEventType.SeasonOpeningConfirmed,
            },
            NotificationFrequency.WeeklyDigest,
            new[] { NotificationChannel.Email },
            NowUtc);

        Assert.Equal("user-1", subscription.UserId);
        Assert.Equal("park-1", subscription.TargetId);
        Assert.Equal(CollectionTargetType.Park, subscription.TargetType);
        Assert.Equal(2, subscription.EventTypes.Count);
        Assert.Equal(NotificationFrequency.WeeklyDigest, subscription.Frequency);
        Assert.Contains(NotificationChannel.Email, subscription.Channels);
        Assert.False(subscription.IsPaused);
        Assert.Equal(1, subscription.Version);
        Assert.Equal(NowUtc, subscription.CreatedAtUtc);
        Assert.Equal(NowUtc, subscription.UpdatedAtUtc);
    }

    [Fact]
    public void Create_ShouldDefensivelyFreezePreferenceSets()
    {
        HashSet<FactualEventType> eventTypes = new()
        {
            FactualEventType.OpeningDateConfirmed,
        };
        HashSet<NotificationChannel> channels = new()
        {
            NotificationChannel.Email,
        };
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.ParkItem,
            "item-1",
            eventTypes,
            NotificationFrequency.DailyDigest,
            channels,
            NowUtc);

        eventTypes.Clear();
        channels.Clear();

        Assert.Contains(FactualEventType.OpeningDateConfirmed, subscription.EventTypes);
        Assert.Contains(NotificationChannel.Email, subscription.Channels);
    }

    [Fact]
    public void Create_WithNoExternalChannel_ShouldKeepThePrivateWebCentreAvailable()
    {
        WatchSubscription subscription = CreateSubscription(
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>());

        Assert.Empty(subscription.Channels);
        Assert.Equal(NotificationFrequency.WebOnly, subscription.Frequency);
    }

    [Fact]
    public void Create_WithNoEventType_ShouldRejectImplicitBroadWatch()
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => WatchSubscription.Create(
                WatchSubscriptionId.Parse("subscription-1"),
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                Array.Empty<FactualEventType>(),
                NotificationFrequency.WebOnly,
                Array.Empty<NotificationChannel>(),
                NowUtc));

        Assert.Equal(WatchSubscriptionErrorCodes.EmptyEventTypes, exception.Code);
    }

    [Fact]
    public void Create_WithEmailAndWebOnlyFrequency_ShouldRejectContradictoryPreference()
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => CreateSubscription(
                NotificationFrequency.WebOnly,
                new[] { NotificationChannel.Email }));

        Assert.Equal(
            WatchSubscriptionErrorCodes.IncompatibleDeliveryPreference,
            exception.Code);
    }

    [Fact]
    public void Create_ForParkItemWithParkOnlyEvent_ShouldRejectAmbiguousScope()
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => WatchSubscription.Create(
                WatchSubscriptionId.Parse("subscription-1"),
                "user-1",
                CollectionTargetType.ParkItem,
                "item-1",
                new[] { FactualEventType.ParkNameChanged },
                NotificationFrequency.WebOnly,
                Array.Empty<NotificationChannel>(),
                NowUtc));

        Assert.Equal(WatchSubscriptionErrorCodes.IncompatibleEventType, exception.Code);
    }

    [Fact]
    public void Create_ForParkWithItemEvent_ShouldExplicitlyCoverChildItems()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.OpeningDateConfirmed },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            NowUtc);

        Assert.True(subscription.Accepts(FactualEventType.OpeningDateConfirmed));
    }

    [Fact]
    public void Create_WithUnknownTargetType_ShouldRejectSubscription()
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => WatchSubscription.Create(
                WatchSubscriptionId.Parse("subscription-1"),
                "user-1",
                (CollectionTargetType)99,
                "target-1",
                new[] { FactualEventType.OpeningDateConfirmed },
                NotificationFrequency.WebOnly,
                Array.Empty<NotificationChannel>(),
                NowUtc));

        Assert.Equal(WatchSubscriptionErrorCodes.InvalidTargetType, exception.Code);
    }

    [Theory]
    [InlineData(0, WatchSubscriptionErrorCodes.InvalidEventType)]
    [InlineData(999, WatchSubscriptionErrorCodes.InvalidEventType)]
    public void Create_WithUnknownEventType_ShouldRejectPreference(
        int rawEventType,
        string expectedCode)
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => WatchSubscription.Create(
                WatchSubscriptionId.Parse("subscription-1"),
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                new[] { (FactualEventType)rawEventType },
                NotificationFrequency.WebOnly,
                Array.Empty<NotificationChannel>(),
                NowUtc));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Theory]
    [InlineData(0, WatchSubscriptionErrorCodes.InvalidFrequency)]
    [InlineData(99, WatchSubscriptionErrorCodes.InvalidFrequency)]
    public void Create_WithUnknownFrequency_ShouldRejectPreference(
        int rawFrequency,
        string expectedCode)
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => WatchSubscription.Create(
                WatchSubscriptionId.Parse("subscription-1"),
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                new[] { FactualEventType.ParkTemporaryClosureConfirmed },
                (NotificationFrequency)rawFrequency,
                Array.Empty<NotificationChannel>(),
                NowUtc));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public void Create_WithUnknownChannel_ShouldRejectPreference()
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => WatchSubscription.Create(
                WatchSubscriptionId.Parse("subscription-1"),
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                new[] { FactualEventType.ParkTemporaryClosureConfirmed },
                NotificationFrequency.DailyDigest,
                new[] { (NotificationChannel)99 },
                NowUtc));

        Assert.Equal(WatchSubscriptionErrorCodes.InvalidChannel, exception.Code);
    }

    [Fact]
    public void UpdatePreferences_ShouldReplaceSelectionWithoutChangingPauseState()
    {
        WatchSubscription subscription = CreateSubscription(
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>());
        subscription.Pause(NowUtc.AddMinutes(1));

        subscription.UpdatePreferences(
            new[]
            {
                FactualEventType.OpeningCalendarChanged,
                FactualEventType.ParkReopeningConfirmed,
            },
            NotificationFrequency.DailyDigest,
            new[] { NotificationChannel.Email },
            NowUtc.AddMinutes(2));

        Assert.True(subscription.IsPaused);
        Assert.Equal(NotificationFrequency.DailyDigest, subscription.Frequency);
        Assert.Equal(2, subscription.EventTypes.Count);
        Assert.Contains(NotificationChannel.Email, subscription.Channels);
        Assert.Equal(3, subscription.Version);
    }

    [Fact]
    public void UpdatePreferences_WithEquivalentSets_ShouldRemainIdempotent()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[]
            {
                FactualEventType.OpeningCalendarChanged,
                FactualEventType.ParkReopeningConfirmed,
            },
            NotificationFrequency.DailyDigest,
            new[] { NotificationChannel.Email },
            NowUtc);

        subscription.UpdatePreferences(
            new[]
            {
                FactualEventType.ParkReopeningConfirmed,
                FactualEventType.OpeningCalendarChanged,
                FactualEventType.OpeningCalendarChanged,
            },
            NotificationFrequency.DailyDigest,
            new[] { NotificationChannel.Email, NotificationChannel.Email },
            NowUtc.AddMinutes(1));

        Assert.Equal(1, subscription.Version);
        Assert.Equal(NowUtc, subscription.UpdatedAtUtc);
    }

    [Fact]
    public void PauseAndResume_ShouldRequireExplicitStateChanges()
    {
        WatchSubscription subscription = CreateSubscription(
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>());

        subscription.Pause(NowUtc.AddMinutes(1));
        subscription.Pause(NowUtc.AddMinutes(2));

        Assert.True(subscription.IsPaused);
        Assert.Equal(2, subscription.Version);
        Assert.False(subscription.Accepts(FactualEventType.ParkTemporaryClosureConfirmed));

        subscription.Resume(NowUtc.AddMinutes(3));
        subscription.Resume(NowUtc.AddMinutes(4));

        Assert.False(subscription.IsPaused);
        Assert.Equal(3, subscription.Version);
        Assert.True(subscription.Accepts(FactualEventType.ParkTemporaryClosureConfirmed));
    }

    [Fact]
    public void Accepts_ShouldRejectAnEventThatWasNotExplicitlySelected()
    {
        WatchSubscription subscription = CreateSubscription(
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>());

        Assert.False(subscription.Accepts(FactualEventType.TicketPricePublishedOrChanged));
    }

    [Fact]
    public void Accepts_ShouldRejectAnEventPublishedBeforeTheMemberSubscribed()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            NowUtc);
        FactualChangeEvent factualEvent = CreatePublishedParkNameEvent(NowUtc.AddMinutes(-1));

        Assert.False(subscription.Accepts(factualEvent));
    }

    [Fact]
    public void Accepts_ShouldIncludeAnEventPublishedWhenTheMemberWasAlreadySubscribed()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            NowUtc);
        FactualChangeEvent factualEvent = CreatePublishedParkNameEvent(NowUtc.AddMinutes(1));

        Assert.True(subscription.Accepts(factualEvent));
    }

    [Fact]
    public void SameTargetForSameOwner_ShouldShareOneLogicalSubscription()
    {
        WatchSubscription first = CreateSubscription(
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>());
        WatchSubscription second = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-2"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.TicketPricePublishedOrChanged },
            NotificationFrequency.WeeklyDigest,
            new[] { NotificationChannel.Email },
            NowUtc);

        Assert.True(first.HasSameLogicalIdentityAs(second));
    }

    [Fact]
    public void Restore_WithInvalidVersion_ShouldRejectPersistedState()
    {
        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => WatchSubscription.Restore(
                WatchSubscriptionId.Parse("subscription-1"),
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                new[] { FactualEventType.ParkTemporaryClosureConfirmed },
                NotificationFrequency.WebOnly,
                Array.Empty<NotificationChannel>(),
                false,
                NowUtc,
                NowUtc,
                0));

        Assert.Equal(WatchSubscriptionErrorCodes.InvalidVersion, exception.Code);
    }

    [Fact]
    public void Pause_WithOlderTimestamp_ShouldRejectMutation()
    {
        WatchSubscription subscription = CreateSubscription(
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>());

        WatchSubscriptionValidationException exception = Assert.Throws<
            WatchSubscriptionValidationException>(() => subscription.Pause(
                NowUtc.AddTicks(-1)));

        Assert.Equal(WatchSubscriptionErrorCodes.InvalidTimestamp, exception.Code);
    }

    private static WatchSubscription CreateSubscription(
        NotificationFrequency frequency,
        IReadOnlyCollection<NotificationChannel> channels)
    {
        return WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkTemporaryClosureConfirmed },
            frequency,
            channels,
            NowUtc);
    }

    private static FactualChangeEvent CreatePublishedParkNameEvent(DateTime publishedAtUtc)
    {
        FactualChangeEvent factualEvent = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-1"),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Ancien nom"),
            FactValue.FromText("Nouveau nom"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Annonce officielle",
                "https://example.com/source",
                NowUtc.AddMinutes(-5)),
            DataConfidence.High,
            NowUtc.AddMinutes(-4),
            "park:park-1:name",
            1,
            NowUtc.AddMinutes(-4));
        factualEvent.Verify(NowUtc.AddMinutes(-2));
        factualEvent.Publish(publishedAtUtc);
        return factualEvent;
    }
}
