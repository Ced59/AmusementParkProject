using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

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
}
