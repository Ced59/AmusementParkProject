using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkOpeningHoursCoverageSegmentBuilderTests
{
    [Fact]
    public void BuildSegments_WhenScheduleHasGap_ShouldSplitCoveredRanges()
    {
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = "park-1",
            TimeZoneId = "UTC",
            RegularRules = new List<ParkOpeningHoursRule>
            {
                new ParkOpeningHoursRule
                {
                    StartDate = new DateOnly(2026, 7, 1),
                    EndDate = new DateOnly(2026, 7, 3),
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Wednesday, DayOfWeek.Friday },
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
        };
        ParkOpeningHoursCoverageSegmentBuilder builder = new ParkOpeningHoursCoverageSegmentBuilder();

        IReadOnlyCollection<ParkOpeningHoursCoverageSegment> segments = builder.BuildSegments(
            schedule,
            new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc));

        Assert.Collection(
            segments,
            first =>
            {
                Assert.Equal(new DateOnly(2026, 7, 1), first.StartDate);
                Assert.Equal(new DateOnly(2026, 7, 1), first.EndDate);
            },
            second =>
            {
                Assert.Equal(new DateOnly(2026, 7, 3), second.StartDate);
                Assert.Equal(new DateOnly(2026, 7, 3), second.EndDate);
            });
    }
}
