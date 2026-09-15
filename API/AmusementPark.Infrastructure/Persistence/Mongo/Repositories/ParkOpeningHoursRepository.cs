using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;
using System.Globalization;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkOpeningHoursRepository : IParkOpeningHoursRepository
{
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
        int maximumCount,
        CancellationToken cancellationToken)
    {
        if (maximumCount < 1 || maximumCount > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        FilterDefinition<ParkOpeningHoursScheduleDocument> filter =
            BuildPendingFactualChangeFilter();
        List<ParkOpeningHoursScheduleDocument> documents = await this.collection
            .Find(filter)
            .SortBy(static document => document.UpdatedAt)
            .Limit(maximumCount)
            .Project(static document => new ParkOpeningHoursScheduleDocument
            {
                ParkId = document.ParkId,
                PendingFactualChanges = document.PendingFactualChanges,
            })
            .ToListAsync(cancellationToken);
        return documents
            .SelectMany(static document => document.PendingFactualChanges.Select(
                entry => new ParkOpeningHoursPendingFactualChange(
                    document.ParkId,
                    entry.ToDomain())))
            .Take(maximumCount)
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
        DateTime now = DateTime.UtcNow;
        ParkOpeningHoursScheduleDocument? existing = await this.collection
            .Find(item => item.ParkId == schedule.ParkId)
            .Project(static item => new ParkOpeningHoursScheduleDocument
            {
                Id = item.Id,
                CreatedAt = item.CreatedAt,
                LastCoverageThirtyDaysNotificationLocalDate = item.LastCoverageThirtyDaysNotificationLocalDate,
                LastCoverageExpiredNotificationLocalDate = item.LastCoverageExpiredNotificationLocalDate,
                FactualRevision = item.FactualRevision,
                PendingFactualChanges = item.PendingFactualChanges,
            })
            .FirstOrDefaultAsync(cancellationToken);

        ParkOpeningHoursScheduleDocument document = schedule.ToDocument();
        document.Id = existing?.Id ?? Guid.NewGuid().ToString("N");
        document.CreatedAt = existing?.CreatedAt ?? now;
        document.UpdatedAt = now;
        document.LastCoverageThirtyDaysNotificationLocalDate = existing?.LastCoverageThirtyDaysNotificationLocalDate;
        document.LastCoverageExpiredNotificationLocalDate = existing?.LastCoverageExpiredNotificationLocalDate;
        document.FactualRevision = existing?.FactualRevision ?? 0;
        document.PendingFactualChanges = existing?.PendingFactualChanges
            ?? new List<FactualChangeOutboxDocument>();

        ParkOpeningHoursPendingFactualChange? pendingFactualChange = null;
        if (factualChange is not null)
        {
            document.FactualRevision = checked(document.FactualRevision + 1);
            FactualChangeCaptureRequest captureRequest = factualChange.ToCaptureRequest(
                document.FactualRevision,
                now);
            FactualChangeOutboxEntry entry = FactualChangeOutboxEntry.Create(captureRequest)
                ?? throw new InvalidOperationException(
                    "A factual write cannot persist an empty opening-hours diff.");
            document.PendingFactualChanges.Add(entry.ToDocument());
            pendingFactualChange = new ParkOpeningHoursPendingFactualChange(
                document.ParkId,
                entry);
        }

        await this.collection.ReplaceOneAsync(
            item => item.ParkId == document.ParkId,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return new ParkOpeningHoursFactualWriteResult(
            document.ToDomain(),
            pendingFactualChange);
    }

    internal static FilterDefinition<ParkOpeningHoursScheduleDocument> BuildPendingFactualChangeFilter()
    {
        return Builders<ParkOpeningHoursScheduleDocument>.Filter.Exists(
            "pendingFactualChanges.0",
            true);
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
