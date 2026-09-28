using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LivePollingActiveWindowTests
{
    [Fact]
    public void Contains_DuringConfiguredHours_ShouldReturnTrue()
    {
        LivePollingActiveWindow window = new LivePollingActiveWindow(TimeZoneInfo.Utc, 6, 23);

        bool contains = window.Contains(
            new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(contains);
    }

    [Fact]
    public void GetNextOpeningUtc_AfterClosing_ShouldReturnNextLocalMorning()
    {
        LivePollingActiveWindow window = new LivePollingActiveWindow(TimeZoneInfo.Utc, 6, 23);
        DateTime afterClosingUtc = new DateTime(2026, 9, 29, 23, 30, 0, DateTimeKind.Utc);

        DateTime nextOpeningUtc = window.GetNextOpeningUtc(afterClosingUtc);

        Assert.Equal(
            new DateTime(2026, 9, 30, 6, 0, 0, DateTimeKind.Utc),
            nextOpeningUtc);
    }

    [Fact]
    public void GetNextOpeningUtc_BeforeOpening_ShouldReturnSameLocalMorning()
    {
        LivePollingActiveWindow window = new LivePollingActiveWindow(TimeZoneInfo.Utc, 6, 23);
        DateTime beforeOpeningUtc = new DateTime(2026, 9, 29, 2, 0, 0, DateTimeKind.Utc);

        DateTime nextOpeningUtc = window.GetNextOpeningUtc(beforeOpeningUtc);

        Assert.Equal(
            new DateTime(2026, 9, 29, 6, 0, 0, DateTimeKind.Utc),
            nextOpeningUtc);
    }
}
