using AmusementPark.Application.Features.ParkOpeningHours.Contracts;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
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
        DateTime now = DateTime.UtcNow;
        ParkOpeningHoursScheduleDocument? existing = await this.collection
            .Find(item => item.ParkId == schedule.ParkId)
            .Project(static item => new ParkOpeningHoursScheduleDocument
            {
                Id = item.Id,
                CreatedAt = item.CreatedAt,
                LastCoverageThirtyDaysNotificationLocalDate = item.LastCoverageThirtyDaysNotificationLocalDate,
                LastCoverageExpiredNotificationLocalDate = item.LastCoverageExpiredNotificationLocalDate,
            })
            .FirstOrDefaultAsync(cancellationToken);

        ParkOpeningHoursScheduleDocument document = schedule.ToDocument();
        document.Id = existing?.Id ?? Guid.NewGuid().ToString("N");
        document.CreatedAt = existing?.CreatedAt ?? now;
        document.UpdatedAt = now;
        document.LastCoverageThirtyDaysNotificationLocalDate = existing?.LastCoverageThirtyDaysNotificationLocalDate;
        document.LastCoverageExpiredNotificationLocalDate = existing?.LastCoverageExpiredNotificationLocalDate;

        await this.collection.ReplaceOneAsync(
            item => item.ParkId == document.ParkId,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return document.ToDomain();
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
