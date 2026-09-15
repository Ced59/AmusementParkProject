using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Rewrites every persisted opening-calendar fact to the contextual evidence schema.
/// Historical states that cannot be reconstructed are explicitly marked unavailable.
/// </summary>
internal sealed class OpeningCalendarFactualEvidenceMigration
{
    private const int BatchSize = 100;

    private readonly IMongoCollection<FactualChangeEventDocument> events;
    private readonly IMongoCollection<FactualChangeOutboxDocument> outbox;
    private readonly IMongoCollection<ParkOpeningHoursScheduleDocument> schedules;

    public OpeningCalendarFactualEvidenceMigration(
        IMongoCollection<FactualChangeEventDocument> events,
        IMongoCollection<FactualChangeOutboxDocument> outbox,
        IMongoCollection<ParkOpeningHoursScheduleDocument> schedules)
    {
        this.events = events ?? throw new ArgumentNullException(nameof(events));
        this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        this.schedules = schedules ?? throw new ArgumentNullException(nameof(schedules));
    }

    public async Task<long> MigrateAsync(CancellationToken cancellationToken)
    {
        long migratedEvents = await this.MigrateEventsAsync(cancellationToken);
        long migratedOutboxEntries = await this.MigrateOutboxAsync(cancellationToken);
        long migratedEmbeddedEntries = await this.MigrateEmbeddedOutboxAsync(cancellationToken);
        return migratedEvents + migratedOutboxEntries + migratedEmbeddedEntries;
    }

    internal static bool MigrateValue(
        FactValueDocument? value,
        ParkOpeningHoursSchedule? currentSchedule)
    {
        if (value is null)
        {
            return false;
        }

        FactValue current = FactValue.Restore(value.Kind, value.CanonicalValue, value.UnitCode);
        FactValue migrated = OpeningCalendarFactSnapshot.MigrateLegacy(current, currentSchedule);
        if (migrated == current)
        {
            return false;
        }

        value.Kind = migrated.Kind;
        value.CanonicalValue = migrated.CanonicalValue;
        value.UnitCode = migrated.UnitCode;
        return true;
    }

    private async Task<long> MigrateEventsAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<FactualChangeEventDocument> filter = BuildCandidateFilter<FactualChangeEventDocument>(
            static document => document.Type,
            "previousValue.canonicalValue",
            "newValue.canonicalValue");
        IFindFluent<FactualChangeEventDocument, FactualChangeEventDocument> find = this.events.Find(filter);
        find.Options.BatchSize = BatchSize;
        long migratedCount = 0;
        using IAsyncCursor<FactualChangeEventDocument> cursor = await find.ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            FactualChangeEventDocument[] batch = cursor.Current.ToArray();
            IReadOnlyDictionary<string, ParkOpeningHoursSchedule> schedulesByPark =
                await this.LoadSchedulesAsync(
                    batch.Select(static document => document.Target.TargetId),
                    cancellationToken);
            List<WriteModel<FactualChangeEventDocument>> writes = new List<WriteModel<FactualChangeEventDocument>>();
            foreach (FactualChangeEventDocument document in batch)
            {
                _ = schedulesByPark.TryGetValue(document.Target.TargetId, out ParkOpeningHoursSchedule? schedule);
                bool changed = MigrateValue(document.PreviousValue, schedule);
                changed |= MigrateValue(document.NewValue, schedule);
                if (!changed)
                {
                    continue;
                }

                writes.Add(new UpdateOneModel<FactualChangeEventDocument>(
                    Builders<FactualChangeEventDocument>.Filter.Eq(static candidate => candidate.Id, document.Id)
                    & Builders<FactualChangeEventDocument>.Filter.Eq(static candidate => candidate.Version, document.Version),
                    Builders<FactualChangeEventDocument>.Update
                        .Set(static candidate => candidate.PreviousValue, document.PreviousValue)
                        .Set(static candidate => candidate.NewValue, document.NewValue)));
            }

            migratedCount += await BulkWriteAsync(this.events, writes, cancellationToken);
        }

        return migratedCount;
    }

    private async Task<long> MigrateOutboxAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<FactualChangeOutboxDocument> filter = BuildCandidateFilter<FactualChangeOutboxDocument>(
            static document => document.Type,
            "previousValue.canonicalValue",
            "newValue.canonicalValue");
        IFindFluent<FactualChangeOutboxDocument, FactualChangeOutboxDocument> find = this.outbox.Find(filter);
        find.Options.BatchSize = BatchSize;
        long migratedCount = 0;
        using IAsyncCursor<FactualChangeOutboxDocument> cursor = await find.ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            FactualChangeOutboxDocument[] batch = cursor.Current.ToArray();
            IReadOnlyDictionary<string, ParkOpeningHoursSchedule> schedulesByPark =
                await this.LoadSchedulesAsync(
                    batch.Select(static document => document.Target.TargetId),
                    cancellationToken);
            List<WriteModel<FactualChangeOutboxDocument>> writes = new List<WriteModel<FactualChangeOutboxDocument>>();
            foreach (FactualChangeOutboxDocument document in batch)
            {
                _ = schedulesByPark.TryGetValue(document.Target.TargetId, out ParkOpeningHoursSchedule? schedule);
                bool changed = MigrateValue(document.PreviousValue, schedule);
                changed |= MigrateValue(document.NewValue, schedule);
                if (!changed)
                {
                    continue;
                }

                writes.Add(new UpdateOneModel<FactualChangeOutboxDocument>(
                    Builders<FactualChangeOutboxDocument>.Filter.Eq(static candidate => candidate.Id, document.Id)
                    & Builders<FactualChangeOutboxDocument>.Filter.Eq(static candidate => candidate.Version, document.Version),
                    Builders<FactualChangeOutboxDocument>.Update
                        .Set(static candidate => candidate.PreviousValue, document.PreviousValue)
                        .Set(static candidate => candidate.NewValue, document.NewValue)));
            }

            migratedCount += await BulkWriteAsync(this.outbox, writes, cancellationToken);
        }

        return migratedCount;
    }

    private async Task<long> MigrateEmbeddedOutboxAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<ParkOpeningHoursScheduleDocument> filter =
            Builders<ParkOpeningHoursScheduleDocument>.Filter.ElemMatch(
                static document => document.PendingFactualChanges,
                BuildCandidateFilter<FactualChangeOutboxDocument>(
                    static entry => entry.Type,
                    "previousValue.canonicalValue",
                    "newValue.canonicalValue"));
        IFindFluent<ParkOpeningHoursScheduleDocument, ParkOpeningHoursScheduleDocument> find =
            this.schedules.Find(filter);
        find.Options.BatchSize = BatchSize;
        long migratedCount = 0;
        using IAsyncCursor<ParkOpeningHoursScheduleDocument> cursor = await find.ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            List<WriteModel<ParkOpeningHoursScheduleDocument>> writes =
                new List<WriteModel<ParkOpeningHoursScheduleDocument>>();
            foreach (ParkOpeningHoursScheduleDocument document in cursor.Current)
            {
                ParkOpeningHoursSchedule schedule = document.ToDomain();
                bool changed = false;
                foreach (FactualChangeOutboxDocument entry in document.PendingFactualChanges.Where(
                    static entry => IsOpeningCalendarEvent(entry.Type)))
                {
                    changed |= MigrateValue(entry.PreviousValue, schedule);
                    changed |= MigrateValue(entry.NewValue, schedule);
                }

                if (!changed)
                {
                    continue;
                }

                writes.Add(new UpdateOneModel<ParkOpeningHoursScheduleDocument>(
                    Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(
                        static candidate => candidate.Id,
                        document.Id)
                    & Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(
                        static candidate => candidate.WriteRevision,
                        document.WriteRevision),
                    Builders<ParkOpeningHoursScheduleDocument>.Update.Set(
                        static candidate => candidate.PendingFactualChanges,
                        document.PendingFactualChanges)));
            }

            migratedCount += await BulkWriteAsync(this.schedules, writes, cancellationToken);
        }

        return migratedCount;
    }

    private async Task<IReadOnlyDictionary<string, ParkOpeningHoursSchedule>> LoadSchedulesAsync(
        IEnumerable<string> parkIds,
        CancellationToken cancellationToken)
    {
        string[] normalizedIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return new Dictionary<string, ParkOpeningHoursSchedule>(StringComparer.Ordinal);
        }

        List<ParkOpeningHoursScheduleDocument> documents = await this.schedules
            .Find(Builders<ParkOpeningHoursScheduleDocument>.Filter.In(
                static document => document.ParkId,
                normalizedIds))
            .ToListAsync(cancellationToken);
        return documents
            .Select(static document => document.ToDomain())
            .ToDictionary(static schedule => schedule.ParkId, StringComparer.Ordinal);
    }

    private static FilterDefinition<TDocument> BuildEventTypeFilter<TDocument>(
        System.Linq.Expressions.Expression<Func<TDocument, FactualEventType>> field)
    {
        return Builders<TDocument>.Filter.In(
            field,
            new[]
            {
                FactualEventType.OpeningCalendarPublished,
                FactualEventType.OpeningCalendarChanged,
            });
    }

    private static FilterDefinition<TDocument> BuildCandidateFilter<TDocument>(
        System.Linq.Expressions.Expression<Func<TDocument, FactualEventType>> eventTypeField,
        FieldDefinition<TDocument, string> previousValueField,
        FieldDefinition<TDocument, string> newValueField)
    {
        FilterDefinitionBuilder<TDocument> filters = Builders<TDocument>.Filter;
        BsonRegularExpression currentSchema = new BsonRegularExpression(
            "(^|;)snapshot=2(;|$)",
            string.Empty);
        FilterDefinition<TDocument> previousIsLegacy =
            filters.Exists(previousValueField, true)
            & filters.Not(filters.Regex(previousValueField, currentSchema));
        FilterDefinition<TDocument> newIsLegacy =
            filters.Exists(newValueField, true)
            & filters.Not(filters.Regex(newValueField, currentSchema));
        return BuildEventTypeFilter(eventTypeField) & (previousIsLegacy | newIsLegacy);
    }

    private static bool IsOpeningCalendarEvent(FactualEventType type)
    {
        return type is FactualEventType.OpeningCalendarPublished
            or FactualEventType.OpeningCalendarChanged;
    }

    private static async Task<long> BulkWriteAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IReadOnlyCollection<WriteModel<TDocument>> writes,
        CancellationToken cancellationToken)
    {
        if (writes.Count == 0)
        {
            return 0;
        }

        BulkWriteResult<TDocument> result = await collection.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
        return result.ModifiedCount;
    }
}
