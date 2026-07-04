using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkOpeningHoursCalendarBuilderTests
{
    [Fact]
    public void BuildCalendar_WhenDateOverrideExists_ShouldPreferOverrideOverRegularRule()
    {
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = "park-1",
            TimeZoneId = "UTC",
            SourceUrl = "https://example.test/hours",
            RegularRules = new List<ParkOpeningHoursRule>
            {
                new ParkOpeningHoursRule
                {
                    StartDate = new DateOnly(2026, 7, 1),
                    EndDate = new DateOnly(2026, 7, 1),
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Wednesday },
                    TimeRanges = new List<ParkOpeningHoursTimeRange>
                    {
                        new ParkOpeningHoursTimeRange
                        {
                            OpensAt = new TimeOnly(10, 0),
                            ClosesAt = new TimeOnly(18, 0),
                        },
                    },
                },
            },
            DateOverrides = new List<ParkOpeningHoursDateOverride>
            {
                new ParkOpeningHoursDateOverride
                {
                    LocalDate = new DateOnly(2026, 7, 1),
                    IsClosed = true,
                    Reasons = new List<LocalizedText>
                    {
                        new LocalizedText("fr", "Maintenance"),
                    },
                },
            },
        };
        ParkOpeningHoursCalendarBuilder builder = new ParkOpeningHoursCalendarBuilder();

        ParkOpeningHoursCalendar calendar = builder.BuildCalendar(
            schedule,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 1));

        ParkOpeningHoursDay day = Assert.Single(calendar.Days);
        Assert.True(day.IsClosed);
        Assert.Equal("override", day.SourceKind);
        Assert.Empty(day.TimeRanges);
        Assert.Equal("Maintenance", Assert.Single(day.Reasons).Value);
    }
}
