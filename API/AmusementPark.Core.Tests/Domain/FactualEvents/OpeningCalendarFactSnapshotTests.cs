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
        Assert.Contains("snapshot=2", firstSnapshot.CanonicalValue);
        Assert.Contains("entries=R|2026-07-01|2026-07-31|1|O|0|10:00-18:00|1|0", firstSnapshot.CanonicalValue);
        Assert.Contains("entryCount=1", firstSnapshot.CanonicalValue);
    }

    [Fact]
    public void Create_WhenClosingTimeChanges_ShouldProduceDifferentFact()
    {
        FactValue? previous = OpeningCalendarFactSnapshot.Create(
            CreateSchedule(new TimeOnly(18, 0)));
        FactValue? current = OpeningCalendarFactSnapshot.Create(
            CreateSchedule(new TimeOnly(19, 0)));

        Assert.NotEqual(previous, current);
        Assert.Contains("|10:00-18:00|1", previous!.CanonicalValue);
        Assert.Contains("|10:00-19:00|1", current!.CanonicalValue);
    }

    [Fact]
    public void Create_WhenWeekdayChanges_ShouldPreserveDistinctReviewEvidence()
    {
        ParkOpeningHoursSchedule previous = CreateSchedule(new TimeOnly(18, 0));
        ParkOpeningHoursSchedule current = CreateSchedule(new TimeOnly(18, 0));
        current.RegularRules[0].DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Tuesday };

        FactValue? previousSnapshot = OpeningCalendarFactSnapshot.Create(previous);
        FactValue? currentSnapshot = OpeningCalendarFactSnapshot.Create(current);

        Assert.NotEqual(previousSnapshot, currentSnapshot);
        Assert.Contains("|1|O|", previousSnapshot!.CanonicalValue);
        Assert.Contains("|2|O|", currentSnapshot!.CanonicalValue);
    }

    [Fact]
    public void MigrateLegacy_WhenCurrentScheduleMatches_ShouldRestoreCompleteEvidence()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule(new TimeOnly(18, 0));
        FactValue current = Assert.IsType<FactValue>(OpeningCalendarFactSnapshot.Create(schedule));
        string hash = current.CanonicalValue.Split("sha256=")[1];
        FactValue legacy = FactValue.FromText(
            $"timezone=Europe/Paris;coverage=2026-07-01/2026-07-31;rules=1;overrides=0;sha256={hash}");

        FactValue migrated = OpeningCalendarFactSnapshot.MigrateLegacy(legacy, schedule);

        Assert.Equal(current, migrated);
        Assert.Contains("evidence=complete", migrated.CanonicalValue);
    }

    [Fact]
    public void MigrateLegacy_WhenHistoricalScheduleCannotBeRebuilt_ShouldMarkEvidenceUnavailable()
    {
        FactValue legacy = FactValue.FromText(
            "timezone=Europe/Paris;coverage=2026-06-01/2026-06-30;rules=2;overrides=1;sha256=historical");

        FactValue migrated = OpeningCalendarFactSnapshot.MigrateLegacy(legacy, null);

        Assert.Contains("snapshot=2", migrated.CanonicalValue);
        Assert.Contains("coverage=2026-06-01/2026-06-30", migrated.CanonicalValue);
        Assert.Contains("evidence=unavailable", migrated.CanonicalValue);
        Assert.DoesNotContain("windows=", migrated.CanonicalValue);
    }

    [Fact]
    public void Create_WithLargeSchedule_ShouldBoundPresentedEvidenceAndRetainTotalCount()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule(new TimeOnly(18, 0));
        schedule.RegularRules = Enumerable.Range(0, 100)
            .Select(index => new ParkOpeningHoursRule
            {
                StartDate = new DateOnly(2026, 1, 1).AddDays(index),
                EndDate = new DateOnly(2026, 1, 1).AddDays(index),
                DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
                TimeRanges = new List<ParkOpeningHoursTimeRange>
                {
                    new ParkOpeningHoursTimeRange
                    {
                        OpensAt = new TimeOnly(10, 0),
                        ClosesAt = new TimeOnly(18, 0),
                    },
                },
            })
            .ToList();

        FactValue snapshot = Assert.IsType<FactValue>(OpeningCalendarFactSnapshot.Create(schedule));

        Assert.True(snapshot.CanonicalValue.Length <= FactValue.MaximumTextLength);
        Assert.Contains("entryCount=100", snapshot.CanonicalValue);
    }

    [Fact]
    public void CreateChange_WhenLateRuleChanges_ShouldPresentThatRuleBeforeTheCap()
    {
        ParkOpeningHoursSchedule previous = CreateScheduleWithDailyRules(20);
        ParkOpeningHoursSchedule current = CreateScheduleWithDailyRules(20);
        current.RegularRules[19].TimeRanges[0].ClosesAt = new TimeOnly(19, 0);

        (FactValue? previousValue, FactValue? newValue) =
            OpeningCalendarFactSnapshot.CreateChange(previous, current);

        Assert.Contains(
            "entries=R|2026-01-20|2026-01-20|1|O|0|10:00-18:00|1|0",
            previousValue!.CanonicalValue);
        Assert.Contains(
            "entries=R|2026-01-20|2026-01-20|1|O|0|10:00-19:00|1|0",
            newValue!.CanonicalValue);
        Assert.Contains("changedEntryCount=1", previousValue.CanonicalValue);
        Assert.Contains("changedEntryCount=1", newValue.CanonicalValue);
    }

    [Fact]
    public void CreateChange_WhenHiddenOpeningWindowChanges_ShouldMarkTheEntryAsChanged()
    {
        ParkOpeningHoursSchedule previous = CreateScheduleWithManyWindows();
        ParkOpeningHoursSchedule current = CreateScheduleWithManyWindows();
        current.RegularRules[0].TimeRanges[7].ClosesAt = new TimeOnly(15, 45);

        (FactValue? previousValue, FactValue? newValue) =
            OpeningCalendarFactSnapshot.CreateChange(previous, current);

        Assert.Contains("changedEntryCount=1", previousValue!.CanonicalValue);
        Assert.Contains("changedEntryCount=1", newValue!.CanonicalValue);
        Assert.DoesNotContain("15:00-15:45", newValue.CanonicalValue);
        Assert.Contains("|8|0", newValue.CanonicalValue);
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
        Assert.Contains("|O|1|10:00-18:00", previousSnapshot!.CanonicalValue);
        Assert.Contains("|O|2|10:00-18:00", currentSnapshot!.CanonicalValue);
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
        Assert.Contains("|10:00-18:00|1|0", previousSnapshot!.CanonicalValue);
        Assert.Contains("|10:00-20:00|1|0", currentSnapshot!.CanonicalValue);
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

    private static ParkOpeningHoursSchedule CreateScheduleWithDailyRules(int count)
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule(new TimeOnly(18, 0));
        schedule.RegularRules = Enumerable.Range(0, count)
            .Select(index => new ParkOpeningHoursRule
            {
                StartDate = new DateOnly(2026, 1, 1).AddDays(index),
                EndDate = new DateOnly(2026, 1, 1).AddDays(index),
                DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
                TimeRanges = new List<ParkOpeningHoursTimeRange>
                {
                    new ParkOpeningHoursTimeRange
                    {
                        OpensAt = new TimeOnly(10, 0),
                        ClosesAt = new TimeOnly(18, 0),
                    },
                },
            })
            .ToList();
        return schedule;
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

    private static ParkOpeningHoursSchedule CreateScheduleWithManyWindows()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule(new TimeOnly(18, 0));
        schedule.RegularRules[0].TimeRanges = Enumerable.Range(0, 8)
            .Select(index => new ParkOpeningHoursTimeRange
            {
                OpensAt = new TimeOnly(8 + index, 0),
                ClosesAt = new TimeOnly(8 + index, 30),
            })
            .ToList();
        return schedule;
    }
}
