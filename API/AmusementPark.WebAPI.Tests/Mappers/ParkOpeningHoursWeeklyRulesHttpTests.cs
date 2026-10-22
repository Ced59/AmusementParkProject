using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkOpeningHours;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ParkOpeningHoursWeeklyRulesHttpTests
{
    [Fact]
    public void ScheduleAndCalendar_PreserveWeeklyRulesSeparatelyFromDatedDays()
    {
        ParkOpeningHoursScheduleDto input = new()
        {
            TimeZoneId = "Europe/Lisbon",
            WeeklyRules = [new()
            {
                Id = "usual",
                DaysOfWeek = ["Tuesday"],
                TimeRanges = [new() { OpensAt = "11:00", ClosesAt = "20:00" }],
            }],
        };
        ParkOpeningHoursSchedule schedule = input.ToDomainResult("standalone-1").Value!;
        Assert.Equal("Tuesday", Assert.Single(schedule.ToHttp().WeeklyRules).DaysOfWeek.Single());
        ParkOpeningHoursWeeklyRuleResult resultRule = new()
        {
            Id = "usual",
            DaysOfWeek = [DayOfWeek.Tuesday],
            TimeRanges = [new() { OpensAt = new(11, 0), ClosesAt = new(20, 0) }],
        };
        ParkOpeningHoursScheduleResult scheduleResult = new() { WeeklyRules = [resultRule] };
        Assert.Equal("usual", Assert.Single(scheduleResult.ToHttp().WeeklyRules).Id);
        ParkOpeningHoursCalendarResult calendarResult = new()
        {
            FromDate = new(2026, 10, 26),
            ToDate = new(2026, 11, 1),
            WeeklyRules = [resultRule],
        };
        ParkOpeningHoursCalendarDto calendar = calendarResult.ToHttp();

        Assert.Empty(calendar.Days);
        Assert.Null(calendar.FirstDate);
        Assert.Null(calendar.LastDate);
        Assert.Equal("20:00", Assert.Single(Assert.Single(calendar.WeeklyRules).TimeRanges).ClosesAt);
    }

    [Fact]
    public void WeeklyRules_RejectInvalidHttpWeekdays()
    {
        ParkOpeningHoursScheduleDto input = new()
        {
            WeeklyRules = [new() { DaysOfWeek = ["Tuesdayy"] }],
        };

        Assert.False(input.ToDomainResult("standalone-1").IsSuccess);
    }
}
