using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
    public async Task MigrateAsync_DuringRollingDeployment_ShouldMigrateLateWriteBeforeCompletion()
    {
        FactualEventMigrationDocument migrationPlan = new FactualEventMigrationDocument
        {
            Id = OpeningCalendarFactualEvidenceMigration.MigrationId,
            StartedAtUtc = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc),
        };
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        FactValue current = Assert.IsType<FactValue>(OpeningCalendarFactSnapshot.Create(schedule));
        string hash = current.CanonicalValue.Split("sha256=")[1];
        ParkOpeningHoursScheduleDocument lateSchedule = schedule.ToDocument();
        lateSchedule.PendingFactualChanges.Add(new FactualChangeOutboxDocument
        {
            Type = FactualEventType.OpeningCalendarChanged,
            NewValue = new FactValueDocument
            {
                Kind = FactValueKind.Text,
                CanonicalValue =
                    $"timezone=Europe/Paris;coverage=2026-07-01/2026-07-31;rules=1;overrides=0;sha256={hash}",
            },
        });
        Mock<IAsyncCursor<FactualEventMigrationDocument>> candidateMigrationCursor =
            CreateAsyncCursor(new[] { migrationPlan });
        Mock<IAsyncCursor<FactualEventMigrationDocument>> canonicalMigrationCursor =
            CreateAsyncCursor(new[] { migrationPlan });
        Mock<IAsyncCursor<FactualChangeEventDocument>> candidateEventCursor =
            CreateAsyncCursor(Array.Empty<FactualChangeEventDocument>());
        Mock<IAsyncCursor<FactualChangeEventDocument>> canonicalEventCursor =
            CreateAsyncCursor(Array.Empty<FactualChangeEventDocument>());
        Mock<IAsyncCursor<FactualChangeOutboxDocument>> candidateOutboxCursor =
            CreateAsyncCursor(Array.Empty<FactualChangeOutboxDocument>());
        Mock<IAsyncCursor<FactualChangeOutboxDocument>> canonicalOutboxCursor =
            CreateAsyncCursor(Array.Empty<FactualChangeOutboxDocument>());
        Mock<IAsyncCursor<ParkOpeningHoursScheduleDocument>> candidateScheduleCursor =
            CreateAsyncCursor(Array.Empty<ParkOpeningHoursScheduleDocument>());
        Mock<IAsyncCursor<ParkOpeningHoursScheduleDocument>> canonicalScheduleCursor =
            CreateAsyncCursor(new[] { lateSchedule });
        Mock<IMongoCollection<FactualChangeEventDocument>> events =
            new Mock<IMongoCollection<FactualChangeEventDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<FactualChangeOutboxDocument>> outbox =
            new Mock<IMongoCollection<FactualChangeOutboxDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<ParkOpeningHoursScheduleDocument>> schedules =
            new Mock<IMongoCollection<ParkOpeningHoursScheduleDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<FactualEventMigrationDocument>> migrations =
            new Mock<IMongoCollection<FactualEventMigrationDocument>>(MockBehavior.Strict);
        SetupFindSequence(migrations, candidateMigrationCursor, canonicalMigrationCursor);
        SetupFindSequence(events, candidateEventCursor, canonicalEventCursor);
        SetupFindSequence(outbox, candidateOutboxCursor, canonicalOutboxCursor);
        SetupFindSequence(schedules, candidateScheduleCursor, canonicalScheduleCursor);
        schedules.Setup(collection => collection.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<ParkOpeningHoursScheduleDocument>>>(),
                It.Is<BulkWriteOptions>(options => !options.IsOrdered),
                CancellationToken.None))
            .ReturnsAsync(new BulkWriteResult<ParkOpeningHoursScheduleDocument>.Acknowledged(
                1,
                1,
                0,
                0,
                1,
                Array.Empty<WriteModel<ParkOpeningHoursScheduleDocument>>(),
                Array.Empty<BulkWriteUpsert>()));
        migrations.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<FactualEventMigrationDocument>>(),
                It.IsAny<UpdateDefinition<FactualEventMigrationDocument>>(),
                null,
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        OpeningCalendarFactualEvidenceMigration candidateMigration =
            new OpeningCalendarFactualEvidenceMigration(
                events.Object,
                outbox.Object,
                schedules.Object,
                migrations.Object,
                false);
        OpeningCalendarFactualEvidenceMigration canonicalMigration =
            new OpeningCalendarFactualEvidenceMigration(
                events.Object,
                outbox.Object,
                schedules.Object,
                migrations.Object,
                true);

        long candidateCount = await candidateMigration.MigrateAsync(CancellationToken.None);
        migrations.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<FactualEventMigrationDocument>>(),
            It.IsAny<UpdateDefinition<FactualEventMigrationDocument>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        long canonicalCount = await canonicalMigration.MigrateAsync(CancellationToken.None);

        Assert.Equal(0, candidateCount);
        Assert.Equal(1, canonicalCount);
        Assert.Contains(
            "snapshot=2",
            Assert.IsType<FactValueDocument>(lateSchedule.PendingFactualChanges[0].NewValue)
                .CanonicalValue);
        events.VerifyAll();
        outbox.VerifyAll();
        schedules.VerifyAll();
        migrations.VerifyAll();
        candidateEventCursor.VerifyAll();
        canonicalEventCursor.VerifyAll();
        candidateOutboxCursor.VerifyAll();
        canonicalOutboxCursor.VerifyAll();
        candidateScheduleCursor.VerifyAll();
        canonicalScheduleCursor.VerifyAll();
        candidateMigrationCursor.VerifyAll();
        canonicalMigrationCursor.VerifyAll();
    }

    [Fact]
    public async Task MigrateAsync_WhenReviewWinsEventRace_ShouldReloadAndFenceItsReplacement()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        FactualChangeEventDocument beforeReview = CreateLegacyEvent(schedule, 4);
        FactualChangeEventDocument afterReview = CreateLegacyEvent(schedule, 5);
        Mock<IAsyncCursor<FactualEventMigrationDocument>> migrationCursor =
            CreateAsyncCursor(new[]
            {
                new FactualEventMigrationDocument
                {
                    Id = OpeningCalendarFactualEvidenceMigration.MigrationId,
                    StartedAtUtc = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc),
                },
            });
        Mock<IAsyncCursor<FactualChangeEventDocument>> firstEventCursor =
            CreateAsyncCursor(new[] { beforeReview });
        Mock<IAsyncCursor<FactualChangeEventDocument>> secondEventCursor =
            CreateAsyncCursor(new[] { afterReview });
        Mock<IAsyncCursor<FactualChangeOutboxDocument>> outboxCursor =
            CreateAsyncCursor(Array.Empty<FactualChangeOutboxDocument>());
        Mock<IAsyncCursor<ParkOpeningHoursScheduleDocument>> firstScheduleCursor =
            CreateAsyncCursor(new[] { schedule.ToDocument() });
        Mock<IAsyncCursor<ParkOpeningHoursScheduleDocument>> secondScheduleCursor =
            CreateAsyncCursor(new[] { schedule.ToDocument() });
        Mock<IAsyncCursor<ParkOpeningHoursScheduleDocument>> embeddedScheduleCursor =
            CreateAsyncCursor(Array.Empty<ParkOpeningHoursScheduleDocument>());
        Mock<IMongoCollection<FactualChangeEventDocument>> events =
            new Mock<IMongoCollection<FactualChangeEventDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<FactualChangeOutboxDocument>> outbox =
            new Mock<IMongoCollection<FactualChangeOutboxDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<ParkOpeningHoursScheduleDocument>> schedules =
            new Mock<IMongoCollection<ParkOpeningHoursScheduleDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<FactualEventMigrationDocument>> migrations =
            new Mock<IMongoCollection<FactualEventMigrationDocument>>(MockBehavior.Strict);
        SetupFind(migrations, migrationCursor);
        SetupFindSequence(events, firstEventCursor, secondEventCursor);
        SetupFind(outbox, outboxCursor);
        SetupFindSequence(
            schedules,
            firstScheduleCursor,
            secondScheduleCursor,
            embeddedScheduleCursor);
        List<IReadOnlyCollection<WriteModel<FactualChangeEventDocument>>> eventWrites = new();
        events.Setup(collection => collection.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<FactualChangeEventDocument>>>(),
                It.Is<BulkWriteOptions>(options => !options.IsOrdered),
                CancellationToken.None))
            .Callback((
                IEnumerable<WriteModel<FactualChangeEventDocument>> writes,
                BulkWriteOptions _,
                CancellationToken _) => eventWrites.Add(writes.ToArray()))
            .ReturnsAsync(() => CreateBulkWriteResult<FactualChangeEventDocument>(
                eventWrites[^1].Count,
                eventWrites.Count == 1 ? 0 : 1));
        migrations.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<FactualEventMigrationDocument>>(),
                It.IsAny<UpdateDefinition<FactualEventMigrationDocument>>(),
                null,
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        OpeningCalendarFactualEvidenceMigration migration =
            new OpeningCalendarFactualEvidenceMigration(
                events.Object,
                outbox.Object,
                schedules.Object,
                migrations.Object);

        long migratedCount = await migration.MigrateAsync(CancellationToken.None);

        Assert.Equal(1, migratedCount);
        Assert.Equal(2, eventWrites.Count);
        UpdateOneModel<FactualChangeEventDocument> successfulWrite =
            Assert.IsType<UpdateOneModel<FactualChangeEventDocument>>(eventWrites[1].Single());
        BsonDocument update = Render(successfulWrite.Update);
        Assert.Equal(6, update["$set"]["version"].AsInt64);
        events.VerifyAll();
        outbox.VerifyAll();
        schedules.VerifyAll();
        migrations.VerifyAll();
        firstEventCursor.VerifyAll();
        secondEventCursor.VerifyAll();
        outboxCursor.VerifyAll();
        firstScheduleCursor.VerifyAll();
        secondScheduleCursor.VerifyAll();
        embeddedScheduleCursor.VerifyAll();
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

    private static FactualChangeEventDocument CreateLegacyEvent(
        ParkOpeningHoursSchedule schedule,
        long version)
    {
        FactValue current = Assert.IsType<FactValue>(OpeningCalendarFactSnapshot.Create(schedule));
        string hash = current.CanonicalValue.Split("sha256=")[1];
        return new FactualChangeEventDocument
        {
            Id = "event-1",
            Type = FactualEventType.OpeningCalendarChanged,
            Target = new ChangeTargetDocument
            {
                Type = FactualTargetType.Park,
                TargetId = schedule.ParkId,
            },
            NewValue = new FactValueDocument
            {
                Kind = FactValueKind.Text,
                CanonicalValue =
                    $"timezone=Europe/Paris;coverage=2026-07-01/2026-07-31;rules=1;overrides=0;sha256={hash}",
            },
            Version = version,
        };
    }

    private static BsonDocument Render(UpdateDefinition<FactualChangeEventDocument> update)
    {
        IBsonSerializer<FactualChangeEventDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<FactualChangeEventDocument>();
        return update.Render(new RenderArgs<FactualChangeEventDocument>(
            serializer,
            BsonSerializer.SerializerRegistry)).AsBsonDocument;
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

    private static void SetupFindSequence<TDocument>(
        Mock<IMongoCollection<TDocument>> collection,
        params Mock<IAsyncCursor<TDocument>>[] cursors)
    {
        Moq.Language.ISetupSequentialResult<Task<IAsyncCursor<TDocument>>> sequence =
            collection.SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<TDocument>>(),
                It.IsAny<FindOptions<TDocument, TDocument>>(),
                CancellationToken.None));
        foreach (Mock<IAsyncCursor<TDocument>> cursor in cursors)
        {
            sequence = sequence.ReturnsAsync(cursor.Object);
        }
    }

    private static BulkWriteResult<TDocument> CreateBulkWriteResult<TDocument>(
        int requestCount,
        long modifiedCount)
    {
        return new BulkWriteResult<TDocument>.Acknowledged(
            requestCount,
            modifiedCount,
            0,
            0,
            modifiedCount,
            Array.Empty<WriteModel<TDocument>>(),
            Array.Empty<BulkWriteUpsert>());
    }
}
