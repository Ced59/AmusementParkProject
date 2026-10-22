using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class ParkOpeningHoursWeeklyRulesMongoTests
{
    [Fact]
    public void RoundTrip_WeeklyOnlySchedule_PreservesRulesWithoutCreatingDatedBounds()
    {
        ParkOpeningHoursSchedule schedule = new()
        {
            ParkId = "standalone-1",
            TimeZoneId = "Europe/Lisbon",
            WeeklyRules = [new()
            {
                Id = "usual-weekdays",
                DaysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Friday],
                Labels = [new("fr", "Accueil sur réservation")],
                TimeRanges = [new() { OpensAt = new(11, 0), ClosesAt = new(20, 0) }],
            }],
        };

        Assert.True(schedule.ToDocument().HasScheduleData);
        Assert.Null(schedule.ToDocument().FirstDate);
        Assert.Null(schedule.ToDocument().LastDate);
        ParkOpeningHoursSchedule actual = schedule.ToDocument().ToDomain();

        ParkOpeningHoursWeeklyRule rule = Assert.Single(actual.WeeklyRules);
        Assert.Equal("usual-weekdays", rule.Id);
        Assert.Equal(schedule.WeeklyRules[0].DaysOfWeek, rule.DaysOfWeek);
        Assert.Equal("Accueil sur réservation", Assert.Single(rule.Labels).Value);
        Assert.Equal(new TimeOnly(20, 0), Assert.Single(rule.TimeRanges).ClosesAt);
        Assert.Empty(actual.RegularRules);
        Assert.Empty(actual.CoverageSegments);
    }
}
