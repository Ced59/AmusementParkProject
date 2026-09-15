using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class OpeningCalendarFactualEvidenceMigrationTests
{
    [Fact]
    public async Task MigrateAsync_WhenMigrationIsCompleted_ShouldSkipCollectionScans()
    {
        FactualEventMigrationDocument migrationPlan = new FactualEventMigrationDocument
        {
            Id = OpeningCalendarFactualEvidenceMigration.MigrationId,
            StartedAtUtc = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc),
            CompletedAtUtc = new DateTime(2026, 9, 15, 10, 1, 0, DateTimeKind.Utc),
        };
        Mock<IAsyncCursor<FactualEventMigrationDocument>> migrationCursor =
            CreateAsyncCursor(new[] { migrationPlan });
        Mock<IMongoCollection<FactualChangeEventDocument>> events =
            new Mock<IMongoCollection<FactualChangeEventDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<FactualChangeOutboxDocument>> outbox =
            new Mock<IMongoCollection<FactualChangeOutboxDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<ParkOpeningHoursScheduleDocument>> schedules =
            new Mock<IMongoCollection<ParkOpeningHoursScheduleDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<FactualEventMigrationDocument>> migrations =
            new Mock<IMongoCollection<FactualEventMigrationDocument>>(MockBehavior.Strict);
        SetupFind(migrations, migrationCursor);
        OpeningCalendarFactualEvidenceMigration migration =
            new OpeningCalendarFactualEvidenceMigration(
                events.Object,
                outbox.Object,
                schedules.Object,
                migrations.Object);

        long migratedCount = await migration.MigrateAsync(CancellationToken.None);

        Assert.Equal(0, migratedCount);
        events.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
        schedules.VerifyNoOtherCalls();
        migrations.VerifyAll();
        migrationCursor.VerifyAll();
    }

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

    private static void SetupFind<TDocument>(
        Mock<IMongoCollection<TDocument>> collection,
        Mock<IAsyncCursor<TDocument>> cursor)
    {
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<TDocument>>(),
                It.IsAny<FindOptions<TDocument, TDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
    }

    private static Mock<IAsyncCursor<TDocument>> CreateAsyncCursor<TDocument>(
        params IReadOnlyCollection<TDocument>[] batches)
    {
        int index = -1;
        Mock<IAsyncCursor<TDocument>> cursor =
            new Mock<IAsyncCursor<TDocument>>(MockBehavior.Strict);
        cursor.Setup(value => value.MoveNextAsync(CancellationToken.None))
            .ReturnsAsync(() =>
            {
                index++;
                return index < batches.Length;
            });
        cursor.SetupGet(value => value.Current).Returns(() => batches[index]);
        cursor.Setup(value => value.Dispose());
        return cursor;
    }
}
