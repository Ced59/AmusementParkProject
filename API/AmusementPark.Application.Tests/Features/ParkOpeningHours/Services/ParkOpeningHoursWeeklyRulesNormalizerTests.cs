using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursWeeklyRulesNormalizerTests
{
    [Fact]
    public void Normalize_WeeklyOnlySchedule_PreservesSplitHoursWithoutDates()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        schedule.WeeklyRules[0].TimeRanges =
        [
            new() { OpensAt = new(10, 0), ClosesAt = new(12, 0) },
            new() { OpensAt = new(14, 0), ClosesAt = new(18, 0) },
        ];

        ParkOpeningHoursSchedule? normalized = new ParkOpeningHoursScheduleNormalizer().Normalize(schedule).Value;

        Assert.NotNull(normalized);
        Assert.Empty(normalized.RegularRules);
        Assert.Empty(normalized.DateOverrides);
        ParkOpeningHoursWeeklyRule rule = Assert.Single(normalized.WeeklyRules);
        Assert.NotEmpty(rule.Id!);
        Assert.Equal(2, rule.TimeRanges.Count);
        Assert.Equal(new[] { DayOfWeek.Tuesday }, rule.DaysOfWeek);
        Assert.Equal(rule.Id, Assert.Single(normalized.ToScheduleResult().WeeklyRules).Id);
        Assert.Equal(2, Assert.Single(new ParkOpeningHoursCalendarBuilder()
            .BuildCalendar(normalized, new(2026, 10, 26), new(2026, 11, 1))
            .ToCalendarResult().WeeklyRules).TimeRanges.Count);
    }

    [Theory]
    [InlineData("empty-days")]
    [InlineData("invalid-day")]
    [InlineData("overlapping-days")]
    [InlineData("empty-ranges")]
    [InlineData("invalid-range")]
    public void Normalize_RejectsInvalidWeeklyRules(string problem)
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        switch (problem)
        {
            case "empty-days": schedule.WeeklyRules[0].DaysOfWeek.Clear(); break;
            case "invalid-day": schedule.WeeklyRules[0].DaysOfWeek = [(DayOfWeek)9]; break;
            case "overlapping-days": schedule.WeeklyRules.Add(CreateSchedule().WeeklyRules[0]); break;
            case "empty-ranges": schedule.WeeklyRules[0].TimeRanges.Clear(); break;
            case "invalid-range": schedule.WeeklyRules[0].TimeRanges[0].ClosesAt = new(9, 0); break;
        }

        Assert.False(new ParkOpeningHoursScheduleNormalizer().Normalize(schedule).IsSuccess);
    }

    [Fact]
    public void Normalize_ExplicitWeeklyClosure_DoesNotNeedOpeningRanges()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        schedule.WeeklyRules[0].IsClosed = true;

        ParkOpeningHoursSchedule? normalized = new ParkOpeningHoursScheduleNormalizer().Normalize(schedule).Value;

        Assert.NotNull(normalized);
        Assert.True(normalized.WeeklyRules[0].IsClosed);
        Assert.Empty(normalized.WeeklyRules[0].TimeRanges);
    }

    private static ParkOpeningHoursSchedule CreateSchedule() => new()
    {
        ParkId = "standalone-1",
        TimeZoneId = "Europe/Lisbon",
        WeeklyRules = [new()
        {
            DaysOfWeek = [DayOfWeek.Tuesday],
            TimeRanges = [new() { OpensAt = new(11, 0), ClosesAt = new(20, 0) }],
        }],
    };
}
