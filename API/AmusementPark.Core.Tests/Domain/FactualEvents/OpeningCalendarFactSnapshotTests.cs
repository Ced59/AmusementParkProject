using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class OpeningCalendarFactSnapshotTests
{
    [Fact]
    public void Create_WithEditorialOnlyChanges_ShouldProduceSameFact()
    {
        ParkOpeningHoursSchedule first = CreateSchedule(new TimeOnly(18, 0));
        first.RegularRules[0].Labels = new List<LocalizedText>
        {
            new LocalizedText("fr", "Saison"),
        };
        ParkOpeningHoursSchedule second = CreateSchedule(new TimeOnly(18, 0));
        second.RegularRules[0].Labels = new List<LocalizedText>
        {
            new LocalizedText("fr", "Haute saison"),
        };

        FactValue? firstSnapshot = OpeningCalendarFactSnapshot.Create(first);
        FactValue? secondSnapshot = OpeningCalendarFactSnapshot.Create(second);

        Assert.NotNull(firstSnapshot);
        Assert.Equal(firstSnapshot, secondSnapshot);
        Assert.Contains("coverage=2026-07-01/2026-07-31", firstSnapshot.CanonicalValue);
    }

    [Fact]
    public void Create_WhenClosingTimeChanges_ShouldProduceDifferentFact()
    {
        FactValue? previous = OpeningCalendarFactSnapshot.Create(
            CreateSchedule(new TimeOnly(18, 0)));
        FactValue? current = OpeningCalendarFactSnapshot.Create(
            CreateSchedule(new TimeOnly(19, 0)));

        Assert.NotEqual(previous, current);
    }

    [Fact]
    public void Create_WhenRulePriorityChanges_ShouldProduceDifferentFact()
    {
        ParkOpeningHoursSchedule previous = CreateSchedule(new TimeOnly(18, 0));
        previous.RegularRules[0].SortOrder = 1;
        ParkOpeningHoursSchedule current = CreateSchedule(new TimeOnly(18, 0));
        current.RegularRules[0].SortOrder = 2;

        FactValue? previousSnapshot = OpeningCalendarFactSnapshot.Create(previous);
        FactValue? currentSnapshot = OpeningCalendarFactSnapshot.Create(current);

        Assert.NotEqual(previousSnapshot, currentSnapshot);
    }

    [Fact]
    public void Create_WhenEqualPriorityRulesSwap_ShouldProduceDifferentFact()
    {
        ParkOpeningHoursSchedule previous = CreateSchedule(new TimeOnly(18, 0));
        previous.RegularRules[0].SortOrder = 1;
        previous.RegularRules.Add(CreateTiedRule(new TimeOnly(20, 0)));
        ParkOpeningHoursSchedule current = CreateSchedule(new TimeOnly(18, 0));
        current.RegularRules[0].SortOrder = 1;
        current.RegularRules.Insert(0, CreateTiedRule(new TimeOnly(20, 0)));

        FactValue? previousSnapshot = OpeningCalendarFactSnapshot.Create(previous);
        FactValue? currentSnapshot = OpeningCalendarFactSnapshot.Create(current);

        Assert.NotEqual(previousSnapshot, currentSnapshot);
    }

    [Fact]
    public void Create_WithoutTimeZone_ShouldNotProduceFact()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule(new TimeOnly(18, 0));
        schedule.TimeZoneId = " ";

        Assert.Null(OpeningCalendarFactSnapshot.Create(schedule));
    }

    [Fact]
    public void Create_WithoutCalendarData_ShouldNotProducePublicationFact()
    {
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = "park-1",
            TimeZoneId = "Europe/Paris",
        };

        Assert.Null(OpeningCalendarFactSnapshot.Create(schedule));
    }

    private static ParkOpeningHoursSchedule CreateSchedule(TimeOnly closesAt)
    {
        return new ParkOpeningHoursSchedule
        {
            ParkId = "park-1",
            TimeZoneId = "Europe/Paris",
            RegularRules = new List<ParkOpeningHoursRule>
            {
                new ParkOpeningHoursRule
                {
                    StartDate = new DateOnly(2026, 7, 1),
                    EndDate = new DateOnly(2026, 7, 31),
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
                    TimeRanges = new List<ParkOpeningHoursTimeRange>
                    {
                        new ParkOpeningHoursTimeRange
                        {
                            OpensAt = new TimeOnly(10, 0),
                            ClosesAt = closesAt,
                        },
                    },
                },
            },
        };
    }

    private static ParkOpeningHoursRule CreateTiedRule(TimeOnly closesAt)
    {
        return new ParkOpeningHoursRule
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31),
            DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
            SortOrder = 1,
            TimeRanges = new List<ParkOpeningHoursTimeRange>
            {
                new ParkOpeningHoursTimeRange
                {
                    OpensAt = new TimeOnly(10, 0),
                    ClosesAt = closesAt,
                },
            },
        };
    }
}
