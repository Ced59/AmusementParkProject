using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkOpeningHoursWeeklyRulesTests
{
    [Fact]
    public void WeeklyPattern_DoesNotInventDatesCoverageOrMissingWeekdays()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        ParkOpeningHoursCalendar calendar = new ParkOpeningHoursCalendarBuilder().BuildCalendar(
            schedule, new DateOnly(2026, 10, 1), new DateOnly(2027, 10, 1));

        Assert.Empty(calendar.Days);
        Assert.Null(calendar.FirstDate);
        Assert.Null(calendar.LastDate);
        ParkOpeningHoursWeeklyRule rule = Assert.Single(calendar.WeeklyRules);
        Assert.Equal(new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday }, rule.DaysOfWeek);
        Assert.DoesNotContain(DayOfWeek.Monday, rule.DaysOfWeek);
        Assert.Empty(new ParkOpeningHoursCoverageSegmentBuilder().BuildSegments(schedule));
    }

    [Fact]
    public void DatedException_IsRetainedSeparatelyFromUsualHours()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        schedule.DateOverrides.Add(new() { LocalDate = new(2026, 10, 27), IsClosed = true });

        ParkOpeningHoursCalendar calendar = new ParkOpeningHoursCalendarBuilder().BuildCalendar(
            schedule, new DateOnly(2026, 10, 26), new DateOnly(2026, 11, 1));

        ParkOpeningHoursDay day = Assert.Single(calendar.Days);
        Assert.Equal(new DateOnly(2026, 10, 27), day.LocalDate);
        Assert.True(day.IsClosed);
        Assert.Equal("override", day.SourceKind);
        Assert.Single(calendar.WeeklyRules);
        Assert.Equal(day.LocalDate, calendar.FirstDate);
        Assert.Equal(day.LocalDate, calendar.LastDate);
    }

    [Fact]
    public void RecentSourcedWeeklyHours_CountForStandaloneWithoutDatedCoverage()
    {
        StandaloneCompletenessFixture fixture = new();
        ParkOpeningHoursSchedule hours = fixture.Context.OpeningHours!;
        hours.RegularRules.Clear();
        hours.WeeklyRules = CreateSchedule().WeeklyRules;

        Assert.Equal(100, fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).CompletenessScore);
        hours.LastVerifiedAtUtc = StandaloneCompletenessFixture.AsOf.AddDays(-91);
        Assert.True(fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).CompletenessScore < 95);
    }

    [Fact]
    public void WeeklyLabels_AreIncludedInPublicTextQuality()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Context.OpeningHours!.WeeklyRules = CreateSchedule().WeeklyRules;
        fixture.Context.OpeningHours.WeeklyRules[0].Reasons = StandaloneCompletenessFixture.Texts("public page");

        Assert.Equal(DataCompletenessScoringRules.ForbiddenPublicTextPublicationBlocker,
            fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).PublicationBlocker);
    }

    private static ParkOpeningHoursSchedule CreateSchedule() => new()
    {
        ParkId = "standalone-1",
        TimeZoneId = "Europe/Lisbon",
        WeeklyRules = [new()
        {
            DaysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Wednesday],
            TimeRanges = [new() { OpensAt = new(11, 0), ClosesAt = new(20, 0) }],
        }],
    };
}
