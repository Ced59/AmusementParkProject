using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Globalization;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkOpeningHoursRepository : IParkOpeningHoursRepository
{
    private static readonly TimeSpan PendingFactualChangeQueryTimeout = TimeSpan.FromSeconds(10);
    private readonly IMongoCollection<ParkOpeningHoursScheduleDocument> collection;

    public ParkOpeningHoursRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkOpeningHoursScheduleDocument>(settings.ParkOpeningHoursCollectionName);
    }

    public async Task<ParkOpeningHoursSchedule?> GetByParkIdAsync(string parkId, CancellationToken cancellationToken)
    {
        ParkOpeningHoursScheduleDocument? document = await this.collection
            .Find(item => item.ParkId == parkId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<ParkOpeningHoursSchedule>> GetByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);

        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<ParkOpeningHoursSchedule>();
        }

        FilterDefinition<ParkOpeningHoursScheduleDocument> filter = Builders<ParkOpeningHoursScheduleDocument>.Filter.In(
            static document => document.ParkId,
            normalizedParkIds);
        List<ParkOpeningHoursScheduleDocument> documents = await this.collection
            .Find(filter)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<ParkOpeningHoursSchedule>> GetPublicTextByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);

        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<ParkOpeningHoursSchedule>();
        }

        FilterDefinition<ParkOpeningHoursScheduleDocument> filter = Builders<ParkOpeningHoursScheduleDocument>.Filter.In(
            static document => document.ParkId,
            normalizedParkIds);
        ProjectionDefinition<ParkOpeningHoursScheduleDocument> projection = Builders<ParkOpeningHoursScheduleDocument>.Projection
            .Include(static document => document.ParkId)
            .Include("regularRules.startDate")
            .Include("regularRules.endDate")
            .Include("regularRules.labels")
            .Include("regularRules.reasons")
            .Include("dateOverrides.localDate")
            .Include("dateOverrides.labels")
            .Include("dateOverrides.reasons");
        List<ParkOpeningHoursScheduleDocument> documents = await this.collection
            .Find(filter)
            .Project<ParkOpeningHoursScheduleDocument>(projection)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary>> GetSummariesByParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkIds.Count == 0)
        {
            return new Dictionary<string, ParkOpeningHoursScheduleSummary>(StringComparer.Ordinal);
        }

        List<ParkOpeningHoursScheduleDocument> documents = await this.collection
            .Find(item => normalizedParkIds.Contains(item.ParkId))
            .Project(static item => new ParkOpeningHoursScheduleDocument
            {
                ParkId = item.ParkId,
                TimeZoneId = item.TimeZoneId,
                SourceUrl = item.SourceUrl,
                LastVerifiedAtUtc = item.LastVerifiedAtUtc,
                FirstDate = item.FirstDate,
                LastDate = item.LastDate,
                HasScheduleData = item.HasScheduleData,
                DateOverrides = item.DateOverrides,
                CoverageSegments = item.CoverageSegments,
                UpdatedAt = item.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return documents
            .Where(static document => !string.IsNullOrWhiteSpace(document.ParkId))
            .GroupBy(static document => document.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group =>
                {
                    ParkOpeningHoursScheduleDocument document = group.First();
                    return ToSummary(document);
                },
                StringComparer.Ordinal);
    }

    public async Task<IReadOnlyCollection<ParkOpeningHoursScheduleSummary>> GetConfiguredSummariesAsync(CancellationToken cancellationToken)
    {
        List<ParkOpeningHoursScheduleDocument> documents = await this.collection
            .Find(static item => item.HasScheduleData)
            .Project(static item => new ParkOpeningHoursScheduleDocument
            {
                ParkId = item.ParkId,
                TimeZoneId = item.TimeZoneId,
                SourceUrl = item.SourceUrl,
                LastVerifiedAtUtc = item.LastVerifiedAtUtc,
                FirstDate = item.FirstDate,
                LastDate = item.LastDate,
                HasScheduleData = item.HasScheduleData,
                DateOverrides = item.DateOverrides,
                CoverageSegments = item.CoverageSegments,
                UpdatedAt = item.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return documents
            .Where(static document => !string.IsNullOrWhiteSpace(document.ParkId))
            .Select(static document => ToSummary(document))
            .ToList();
    }

    public async Task<bool> TryMarkCoverageNotificationSentAsync(string parkId, int thresholdDays, DateOnly localDate, CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId.Trim();
        string formattedLocalDate = FormatDate(localDate);
        FilterDefinition<ParkOpeningHoursScheduleDocument> baseFilter =
            Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(static item => item.ParkId, normalizedParkId)
            & Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(static item => item.HasScheduleData, true);

        FilterDefinition<ParkOpeningHoursScheduleDocument> filter;
        UpdateDefinition<ParkOpeningHoursScheduleDocument> update;
        if (thresholdDays == 30)
        {
            filter = baseFilter
                & Builders<ParkOpeningHoursScheduleDocument>.Filter.Ne(static item => item.LastCoverageThirtyDaysNotificationLocalDate, formattedLocalDate);
            update = Builders<ParkOpeningHoursScheduleDocument>.Update.Set(static item => item.LastCoverageThirtyDaysNotificationLocalDate, formattedLocalDate);
        }
        else if (thresholdDays == 0)
        {
            filter = baseFilter
                & Builders<ParkOpeningHoursScheduleDocument>.Filter.Ne(static item => item.LastCoverageExpiredNotificationLocalDate, formattedLocalDate);
            update = Builders<ParkOpeningHoursScheduleDocument>.Update.Set(static item => item.LastCoverageExpiredNotificationLocalDate, formattedLocalDate);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdDays), thresholdDays, "Unsupported opening hours coverage notification threshold.");
        }

        UpdateResult result = await this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<ParkOpeningHoursSchedule> UpsertAsync(ParkOpeningHoursSchedule schedule, CancellationToken cancellationToken)
    {
        ParkOpeningHoursFactualWriteResult result = await this.UpsertCoreAsync(
            schedule,
            null,
            cancellationToken);
        return result.Schedule;
    }

    public Task<ParkOpeningHoursFactualWriteResult> UpsertWithFactualChangeAsync(
        ParkOpeningHoursSchedule schedule,
        ParkOpeningHoursFactualChangeDraft? factualChange,
        CancellationToken cancellationToken)
    {
        return this.UpsertCoreAsync(schedule, factualChange, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParkOpeningHoursPendingFactualChange>> GetPendingFactualChangesAsync(
        ParkOpeningHoursFactualChangeCursor? after,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        if (maximumCount < 1 || maximumCount > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        BsonDocument[] pipeline = BuildPendingFactualChangePipeline(after, maximumCount);
        List<BsonDocument> documents = await this.collection
            .Aggregate<BsonDocument>(
                pipeline,
                new AggregateOptions
                {
                    MaxTime = PendingFactualChangeQueryTimeout,
                })
            .ToListAsync(cancellationToken);
        return documents
            .Select(static document => new ParkOpeningHoursPendingFactualChange(
                document["parkId"].AsString,
                BsonSerializer.Deserialize<FactualChangeOutboxDocument>(
                        document["entry"].AsBsonDocument)
                    .ToDomain(),
                document["sourceUpdatedAtUtc"].AsBsonDateTime.ToUniversalTime()))
            .ToList();
    }

    public async Task<bool> MarkFactualChangeRecordedAsync(
        string parkId,
        string outboxEntryId,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        string normalizedEntryId = outboxEntryId?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0)
        {
            throw new ArgumentException("A park identifier is required.", nameof(parkId));
        }

        if (normalizedEntryId.Length == 0)
        {
            throw new ArgumentException("An outbox entry identifier is required.", nameof(outboxEntryId));
        }

        FilterDefinition<ParkOpeningHoursScheduleDocument> filter =
            Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(
                static document => document.ParkId,
                normalizedParkId)
            & Builders<ParkOpeningHoursScheduleDocument>.Filter.ElemMatch(
                static document => document.PendingFactualChanges,
                entry => entry.Id == normalizedEntryId);
        UpdateDefinition<ParkOpeningHoursScheduleDocument> update =
            BuildMarkFactualChangeRecordedUpdate(normalizedEntryId);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private async Task<ParkOpeningHoursFactualWriteResult> UpsertCoreAsync(
        ParkOpeningHoursSchedule schedule,
        ParkOpeningHoursFactualChangeDraft? factualChange,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 5;
        for (int attempt = 1; attempt <= maximumAttempts; attempt += 1)
        {
            DateTime now = DateTime.UtcNow;
            ParkOpeningHoursScheduleDocument? existing = await this.collection
                .Find(item => item.ParkId == schedule.ParkId)
                .FirstOrDefaultAsync(cancellationToken);
            ParkOpeningHoursScheduleDocument document = schedule.ToDocument();
            document.Id = existing?.Id ?? Guid.NewGuid().ToString("N");
            document.CreatedAt = existing?.CreatedAt ?? now;
            document.UpdatedAt = now;
            document.LastCoverageThirtyDaysNotificationLocalDate = existing?.LastCoverageThirtyDaysNotificationLocalDate;
            document.LastCoverageExpiredNotificationLocalDate = existing?.LastCoverageExpiredNotificationLocalDate;
            document.FactualRevision = existing?.FactualRevision ?? 0;
            document.WriteRevision = checked((existing?.WriteRevision ?? 0) + 1);
            document.PendingFactualChanges = existing?.PendingFactualChanges.ToList()
                ?? new List<FactualChangeOutboxDocument>();

            ParkOpeningHoursPendingFactualChange? pendingFactualChange =
                BuildPendingFactualChange(
                    document,
                    existing,
                    factualChange,
                    now);
            try
            {
                ReplaceOneResult replaceResult = await this.collection.ReplaceOneAsync(
                    BuildWriteRevisionFilter(
                        document.ParkId,
                        existing?.Id,
                        existing?.WriteRevision ?? 0),
                    document,
                    new ReplaceOptions { IsUpsert = existing is null },
                    cancellationToken);
                if (existing is null || replaceResult.MatchedCount == 1)
                {
                    return new ParkOpeningHoursFactualWriteResult(
                        document.ToDomain(),
                        pendingFactualChange);
                }
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey
                    && attempt < maximumAttempts)
            {
            }
        }

        throw new InvalidOperationException(
            $"The opening-hours schedule for park '{schedule.ParkId}' changed concurrently too many times.");
    }

    internal static FilterDefinition<ParkOpeningHoursScheduleDocument> BuildPendingFactualChangeFilter(
        ParkOpeningHoursFactualChangeCursor? after)
    {
        return new BsonDocumentFilterDefinition<ParkOpeningHoursScheduleDocument>(
            BuildPendingFactualChangeSourceMatch(after));
    }

    internal static BsonDocument[] BuildPendingFactualChangePipeline(
        ParkOpeningHoursFactualChangeCursor? after,
        int maximumCount)
    {
        if (maximumCount < 1 || maximumCount > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        List<BsonDocument> pipeline = new List<BsonDocument>
        {
            new BsonDocument("$match", BuildPendingFactualChangeSourceMatch(after)),
            new BsonDocument("$unwind", "$pendingFactualChanges"),
        };
        if (after is not null)
        {
            pipeline.Add(new BsonDocument(
                "$match",
                BuildPendingFactualChangeEntryMatch(after)));
        }

        pipeline.Add(new BsonDocument("$sort", new BsonDocument
        {
            ["updatedAt"] = 1,
            ["parkId"] = 1,
            ["pendingFactualChanges.createdAt"] = 1,
            ["pendingFactualChanges._id"] = 1,
        }));
        pipeline.Add(new BsonDocument("$limit", maximumCount));
        pipeline.Add(new BsonDocument("$project", new BsonDocument
        {
            ["_id"] = 0,
            ["parkId"] = 1,
            ["sourceUpdatedAtUtc"] = "$updatedAt",
            ["entry"] = "$pendingFactualChanges",
        }));
        return pipeline.ToArray();
    }

    private static BsonDocument BuildPendingFactualChangeSourceMatch(
        ParkOpeningHoursFactualChangeCursor? after)
    {
        BsonDocument pending = new BsonDocument(
            "pendingFactualChanges.0",
            new BsonDocument("$exists", true));
        if (after is null)
        {
            return pending;
        }

        return new BsonDocument("$and", new BsonArray
        {
            pending,
            new BsonDocument("$or", new BsonArray
            {
                new BsonDocument(
                    "updatedAt",
                    new BsonDocument("$gt", after.SourceUpdatedAtUtc)),
                new BsonDocument
                {
                    ["updatedAt"] = after.SourceUpdatedAtUtc,
                    ["parkId"] = new BsonDocument("$gte", after.ParkId),
                },
            }),
        });
    }

    private static BsonDocument BuildPendingFactualChangeEntryMatch(
        ParkOpeningHoursFactualChangeCursor after)
    {
        return new BsonDocument("$or", new BsonArray
        {
            new BsonDocument(
                "updatedAt",
                new BsonDocument("$gt", after.SourceUpdatedAtUtc)),
            new BsonDocument
            {
                ["updatedAt"] = after.SourceUpdatedAtUtc,
                ["parkId"] = new BsonDocument("$gt", after.ParkId),
            },
            new BsonDocument
            {
                ["updatedAt"] = after.SourceUpdatedAtUtc,
                ["parkId"] = after.ParkId,
                ["$or"] = new BsonArray
                {
                    new BsonDocument(
                        "pendingFactualChanges.createdAt",
                        new BsonDocument("$gt", after.EntryRecordedAtUtc)),
                    new BsonDocument
                    {
                        ["pendingFactualChanges.createdAt"] = after.EntryRecordedAtUtc,
                        ["pendingFactualChanges._id"] = new BsonDocument("$gt", after.EntryId),
                    },
                },
            },
        });
    }

    internal static FilterDefinition<ParkOpeningHoursScheduleDocument> BuildWriteRevisionFilter(
        string parkId,
        string? documentId,
        long expectedWriteRevision)
    {
        FilterDefinition<ParkOpeningHoursScheduleDocument> identity =
            string.IsNullOrWhiteSpace(documentId)
                ? Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(
                    static document => document.ParkId,
                    parkId)
                : Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(
                    static document => document.Id,
                    documentId);
        FilterDefinition<ParkOpeningHoursScheduleDocument> revision =
            Builders<ParkOpeningHoursScheduleDocument>.Filter.Eq(
                static document => document.WriteRevision,
                expectedWriteRevision);
        if (expectedWriteRevision == 0)
        {
            revision |= Builders<ParkOpeningHoursScheduleDocument>.Filter.Exists(
                static document => document.WriteRevision,
                false);
        }

        return identity & revision;
    }

    private static ParkOpeningHoursPendingFactualChange? BuildPendingFactualChange(
        ParkOpeningHoursScheduleDocument document,
        ParkOpeningHoursScheduleDocument? existing,
        ParkOpeningHoursFactualChangeDraft? factualChange,
        DateTime recordedAtUtc)
    {
        if (factualChange is null)
        {
            return null;
        }

        FactValue? previousValue = OpeningCalendarFactSnapshot.Create(existing?.ToDomain());
        FactValue? newValue = OpeningCalendarFactSnapshot.Create(document.ToDomain());
        if (FactualChangeDiff.Detect(previousValue, newValue) is null)
        {
            return null;
        }

        document.FactualRevision = checked(document.FactualRevision + 1);
        ParkOpeningHoursFactualChangeDraft rebasedChange = factualChange with
        {
            Type = previousValue is null && newValue is not null
                ? FactualEventType.OpeningCalendarPublished
                : FactualEventType.OpeningCalendarChanged,
            PreviousValue = previousValue,
            NewValue = newValue,
        };
        FactualChangeCaptureRequest captureRequest = rebasedChange.ToCaptureRequest(
            document.FactualRevision,
            recordedAtUtc);
        FactualChangeOutboxEntry entry = FactualChangeOutboxEntry.Create(captureRequest)
            ?? throw new InvalidOperationException(
                "A factual write cannot persist an empty opening-hours diff.");
        document.PendingFactualChanges.Add(entry.ToDocument());
        return new ParkOpeningHoursPendingFactualChange(
            document.ParkId,
            entry,
            document.UpdatedAt);
    }

    internal static UpdateDefinition<ParkOpeningHoursScheduleDocument> BuildMarkFactualChangeRecordedUpdate(
        string outboxEntryId)
    {
        return Builders<ParkOpeningHoursScheduleDocument>.Update.PullFilter(
            static document => document.PendingFactualChanges,
            entry => entry.Id == outboxEntryId);
    }

    private static ParkOpeningHoursScheduleSummary ToSummary(ParkOpeningHoursScheduleDocument document)
    {
        return new ParkOpeningHoursScheduleSummary
        {
            ParkId = document.ParkId,
            TimeZoneId = document.TimeZoneId,
            SourceUrl = document.SourceUrl,
            FirstDate = ParseDateOrNull(document.FirstDate),
            LastDate = ParseDateOrNull(document.LastDate),
            LastVerifiedAtUtc = document.LastVerifiedAtUtc,
            UpdatedAtUtc = document.UpdatedAt,
            HasScheduleData = document.HasScheduleData,
            HasDateOverrides = document.DateOverrides.Count > 0,
            CoverageSegments = document.CoverageSegments
                .Select(static segment => ToSummary(segment))
                .Where(static segment => segment is not null)
                .Select(static segment => segment!)
                .ToList(),
        };
    }

    private static ParkOpeningHoursCoverageSegmentSummary? ToSummary(ParkOpeningHoursCoverageSegmentDocument document)
    {
        DateOnly? startDate = ParseDateOrNull(document.StartDate);
        DateOnly? endDate = ParseDateOrNull(document.EndDate);
        if (!startDate.HasValue || !endDate.HasValue || startDate.Value > endDate.Value)
        {
            return null;
        }

        return new ParkOpeningHoursCoverageSegmentSummary
        {
            StartDate = startDate.Value,
            EndDate = endDate.Value,
        };
    }

    private static string FormatDate(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseDateOrNull(string? value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : null;
    }
}
