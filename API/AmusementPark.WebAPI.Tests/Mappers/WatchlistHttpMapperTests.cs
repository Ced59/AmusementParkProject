using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Contracts.Watchlists;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class WatchlistHttpMapperTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TryToApplication_ShouldParseAnExplicitWebOnlyPreference()
    {
        WatchSubscriptionWriteRequestDto request = new WatchSubscriptionWriteRequestDto
        {
            TargetType = "park",
            TargetId = " park-1 ",
            EventTypes = new[] { "ParkNameChanged", "ParkNameChanged" },
            Frequency = "WebOnly",
            Channels = Array.Empty<string>(),
        };

        bool parsed = request.TryToApplication(out AmusementPark.Application.Features.Watchlists.Models.WatchSubscriptionPreferenceInput? input);

        Assert.True(parsed);
        Assert.Equal("park-1", input?.TargetId);
        Assert.Single(input!.EventTypes);
        Assert.Equal(NotificationFrequency.WebOnly, input.Frequency);
    }

    [Fact]
    public void ToHttp_ShouldExposeReadableParkFiltersAndNavigationTargets()
    {
        UserNotificationResult notification = new UserNotificationResult(
            "notification-1",
            FactualEventType.OpeningDateChanged,
            UserNotificationStatus.Delivered,
            UserNotificationNoticeKind.Update,
            FactualChangeStatus.Published,
            null,
            null,
            new UserNotificationTargetResult(
                FactualTargetType.ParkItem,
                "item-1",
                "park-1",
                "Voltron Nevera",
                "Europa-Park",
                "image-1"),
            new UserNotificationFactValueResult(FactValueKind.Date, "2027-04-01", null),
            new UserNotificationFactValueResult(FactValueKind.Date, "2027-04-08", null),
            new UserNotificationSourceResult(
                SourceReferenceType.OfficialWebsite,
                "Europa-Park",
                "Opening update",
                "https://example.com/opening",
                NowUtc.AddHours(-2),
                NowUtc.AddHours(-1)),
            NowUtc.AddHours(-2),
            NowUtc,
            null,
            NowUtc.AddDays(365),
            1,
            true,
            2);
        UserNotificationPageResult result = new UserNotificationPageResult(
            new[] { notification },
            1,
            12,
            1,
            1,
            365,
            new[] { new UserNotificationParkFilterResult("park-1", "Europa-Park") });

        UserNotificationPageDto dto = result.ToHttp();

        UserNotificationDto item = Assert.Single(dto.Items);
        Assert.Equal("Voltron Nevera", item.Target.Name);
        Assert.Equal("park-1", item.Target.ParkId);
        Assert.Equal("Europa-Park", Assert.Single(dto.ParkFilters).ParkName);
        Assert.Equal("2027-04-08", item.NewValue?.CanonicalValue);
    }
}
