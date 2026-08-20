using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class AutomaticHistoryEventFactoryTests
{
    [Fact]
    public void CreateParkLifecycleEvents_WhenOpeningDateIsTextOnlyMonth_ShouldCreateMonthPrecisionEvent()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Magic Park",
            OpeningDate = null,
            OpeningDateText = "1998-05",
            IsVisible = true,
        };

        IReadOnlyCollection<HistoryEvent> events = AutomaticHistoryEventFactory.CreateParkLifecycleEvents(park);

        HistoryEvent historyEvent = Assert.Single(events);
        Assert.Equal(1998, historyEvent.Year);
        Assert.Equal(5, historyEvent.Month);
        Assert.Null(historyEvent.Day);
        Assert.Equal(HistoryDatePrecision.Month, historyEvent.DatePrecision);
        Assert.Equal(ParkHistoryEventType.Opening.ToString(), historyEvent.EventType);
    }

    [Fact]
    public void CreateParkLifecycleEvents_WhenOpeningDateTextUsesMonthName_ShouldCreateMonthPrecisionEvent()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Magic Park",
            OpeningDate = null,
            OpeningDateText = "juin 1998",
            IsVisible = true,
        };

        IReadOnlyCollection<HistoryEvent> events = AutomaticHistoryEventFactory.CreateParkLifecycleEvents(park);

        HistoryEvent historyEvent = Assert.Single(events);
        Assert.Equal(1998, historyEvent.Year);
        Assert.Equal(6, historyEvent.Month);
        Assert.Null(historyEvent.Day);
        Assert.Equal(HistoryDatePrecision.Month, historyEvent.DatePrecision);
    }

    [Fact]
    public void CreateParkLifecycleEvents_WhenDateTextKeepsMonthPrecision_ShouldNotPromoteToDay()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Magic Park",
            OpeningDate = new DateTime(1998, 6, 1),
            OpeningDateText = "June 1998",
            IsVisible = true,
        };

        IReadOnlyCollection<HistoryEvent> events = AutomaticHistoryEventFactory.CreateParkLifecycleEvents(park);

        HistoryEvent historyEvent = Assert.Single(events);
        Assert.Equal(1998, historyEvent.Year);
        Assert.Equal(6, historyEvent.Month);
        Assert.Null(historyEvent.Day);
        Assert.Equal(HistoryDatePrecision.Month, historyEvent.DatePrecision);
    }

    [Fact]
    public void CreateParkItemLifecycleEvents_WhenClosingDateIsTextOnlyYear_ShouldCreateYearPrecisionEvent()
    {
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Old Ride",
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                ClosingDate = null,
                ClosingDateText = "2004",
            },
        };

        IReadOnlyCollection<HistoryEvent> events = AutomaticHistoryEventFactory.CreateParkItemLifecycleEvents(item);

        HistoryEvent historyEvent = Assert.Single(events);
        Assert.Equal(2004, historyEvent.Year);
        Assert.Null(historyEvent.Month);
        Assert.Null(historyEvent.Day);
        Assert.Equal(HistoryDatePrecision.Year, historyEvent.DatePrecision);
        Assert.Equal(ParkItemHistoryEventType.DefinitiveClosure.ToString(), historyEvent.EventType);
    }

    [Fact]
    public void CreateStandaloneAttractionLifecycleEvents_ShouldKeepStandaloneOwnershipAndSourceTimestamp()
    {
        DateTime updatedAtUtc = new DateTime(2026, 8, 19, 12, 30, 0, DateTimeKind.Utc);
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Alpine Coaster",
            IsVisible = true,
            UpdatedAtUtc = updatedAtUtc,
            AttractionDetails = new AttractionDetails
            {
                OpeningDateText = "2007",
            },
        };

        IReadOnlyCollection<HistoryEvent> events = AutomaticHistoryEventFactory.CreateStandaloneAttractionLifecycleEvents(attraction);

        HistoryEvent historyEvent = Assert.Single(events);
        Assert.Equal("auto-standalone-standalone-1-opening-2007", historyEvent.Key);
        Assert.Equal(HistoryEntityType.StandaloneAttraction, historyEvent.EntityType);
        Assert.Equal("standalone-1", historyEvent.OwnerId);
        Assert.Null(historyEvent.ParkId);
        Assert.Null(historyEvent.ParkItemId);
        Assert.Null(historyEvent.ContextParkId);
        Assert.Equal(updatedAtUtc, historyEvent.UpdatedAtUtc);
    }
}
