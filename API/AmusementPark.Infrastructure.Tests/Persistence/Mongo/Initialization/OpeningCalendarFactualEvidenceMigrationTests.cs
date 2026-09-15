using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class OpeningCalendarFactualEvidenceMigrationTests
{
    [Fact]
    public void MigrateValue_WithMatchingCurrentSchedule_ShouldPersistContextualEvidence()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        FactValue current = Assert.IsType<FactValue>(OpeningCalendarFactSnapshot.Create(schedule));
        string hash = current.CanonicalValue.Split("sha256=")[1];
        FactValueDocument document = new FactValueDocument
        {
            Kind = FactValueKind.Text,
            CanonicalValue =
                $"timezone=Europe/Paris;coverage=2026-07-01/2026-07-31;rules=1;overrides=0;sha256={hash}",
        };

        bool changed = OpeningCalendarFactualEvidenceMigration.MigrateValue(document, schedule);

        Assert.True(changed);
        Assert.Contains("snapshot=2", document.CanonicalValue);
        Assert.Contains("R|2026-07-01|2026-07-31|1|O|0|10:00-18:00|1|0", document.CanonicalValue);
    }

    [Fact]
    public void MigrateValue_WithAlreadyMigratedValue_ShouldBeIdempotent()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        FactValue current = Assert.IsType<FactValue>(OpeningCalendarFactSnapshot.Create(schedule));
        FactValueDocument document = new FactValueDocument
        {
            Kind = current.Kind,
            CanonicalValue = current.CanonicalValue,
            UnitCode = current.UnitCode,
        };

        bool changed = OpeningCalendarFactualEvidenceMigration.MigrateValue(document, schedule);

        Assert.False(changed);
        Assert.Equal(current.CanonicalValue, document.CanonicalValue);
    }

    private static ParkOpeningHoursSchedule CreateSchedule()
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
                            ClosesAt = new TimeOnly(18, 0),
                        },
                    },
                },
            },
        };
    }
}
