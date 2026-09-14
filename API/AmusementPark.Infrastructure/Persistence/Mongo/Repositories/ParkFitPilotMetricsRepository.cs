using System.Globalization;
using System.Text;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkFitPilotMetricsRepository : IParkFitPilotMetricsRepository
{
    private const int RetentionDays = 400;
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<ParkFitPilotDailyMetricsDocument> metrics;
    private readonly IMongoCollection<ParkFitSourceReportDocument> reports;

    public ParkFitPilotMetricsRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.metrics = database.GetCollection<ParkFitPilotDailyMetricsDocument>(
            settings.ParkFitPilotDailyMetricsCollectionName);
        this.reports = database.GetCollection<ParkFitSourceReportDocument>(
            settings.ParkFitSourceReportsCollectionName);
    }

    public async Task IncrementAsync(
        DateOnly dateUtc,
        ParkFitPilotObservation observation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observation);

        DateTime dayUtc = dateUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime updatedAtUtc = DateTime.UtcNow;
        UpdateDefinitionBuilder<ParkFitPilotDailyMetricsDocument> builder =
            Builders<ParkFitPilotDailyMetricsDocument>.Update;
        List<UpdateDefinition<ParkFitPilotDailyMetricsDocument>> updates =
        [
            builder.SetOnInsert(static document => document.Id, ToDateKey(dateUtc)),
            builder.SetOnInsert(static document => document.DateUtc, dayUtc),
            builder.Set(
                static document => document.UpdatedAtUtc,
                updatedAtUtc),
            builder.Set(
                static document => document.ExpiresAtUtc,
                dayUtc.AddDays(RetentionDays)),
            builder.Inc($"eventCounts.{observation.EventKind}", 1L),
        ];
        AddOptionalIncrement(updates, builder, "resultBandCounts", observation.ResultBand);
        AddOptionalIncrement(updates, builder, "unknownLevelCounts", observation.UnknownLevel);
        AddOptionalIncrement(updates, builder, "durationBandCounts", observation.DurationBand);
        AddOptionalIncrement(updates, builder, "failureKindCounts", observation.FailureKind);
        AddOptionalIncrement(updates, builder, "comparisonSizeCounts", observation.ComparisonSize);
        foreach (AmusementPark.Core.Domain.Parks.ParkFitDataQualityIssue issue
            in observation.QualityIssues)
        {
            updates.Add(builder.Inc($"qualityIssueCounts.{issue}", 1L));
            if (observation.ResultBand == ParkFitPilotResultBand.None)
            {
                updates.Add(builder.Inc($"zeroResultQualityIssueCounts.{issue}", 1L));
            }
        }

        if (observation.MethodVersion is not null)
        {
            updates.Add(builder.Inc(
                $"methodVersionCounts.{NormalizeCounterKey(observation.MethodVersion)}",
                1L));
        }

        FilterDefinition<ParkFitPilotDailyMetricsDocument> filter =
            Builders<ParkFitPilotDailyMetricsDocument>.Filter.Eq(
                static document => document.Id,
                ToDateKey(dateUtc));
        await this.metrics.UpdateOneAsync(
            filter,
            builder.Combine(updates),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<ParkFitPilotMetricsSnapshot> ReadAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        DateTime fromDayUtc = DateTime.SpecifyKind(fromUtc.Date, DateTimeKind.Utc);
        DateTime toDayUtc = DateTime.SpecifyKind(toUtc.Date, DateTimeKind.Utc);
        DateTime reportToUtc = toDayUtc == DateTime.SpecifyKind(DateTime.MaxValue.Date, DateTimeKind.Utc)
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : toDayUtc.AddDays(1).AddTicks(-1);
        FilterDefinition<ParkFitPilotDailyMetricsDocument> filter =
            Builders<ParkFitPilotDailyMetricsDocument>.Filter.Gte(
                static document => document.DateUtc,
                fromDayUtc)
            & Builders<ParkFitPilotDailyMetricsDocument>.Filter.Lte(
                static document => document.DateUtc,
                toDayUtc);
        List<ParkFitPilotDailyMetricsDocument> metricDocuments = await this.metrics
            .Find(filter)
            .SortBy(static document => document.DateUtc)
            .ToListAsync(cancellationToken);
        IReadOnlyDictionary<string, (long Total, long Outdated)> reportCounts =
            await this.ReadReportCountsAsync(
                fromDayUtc,
                reportToUtc,
                cancellationToken);
        Dictionary<string, ParkFitPilotDailyMetricsDocument> metricsByDate = metricDocuments
            .ToDictionary(static document => document.Id, StringComparer.Ordinal);
        List<ParkFitPilotDailyMetrics> daily = new();
        DateTime dayUtc = fromDayUtc;
        while (dayUtc <= toDayUtc)
        {
            string date = ToDateKey(DateOnly.FromDateTime(dayUtc));
            metricsByDate.TryGetValue(date, out ParkFitPilotDailyMetricsDocument? document);
            reportCounts.TryGetValue(date, out (long Total, long Outdated) reportCount);
            daily.Add(new ParkFitPilotDailyMetrics(
                date,
                document?.EventCounts ?? EmptyCounts(),
                document?.ResultBandCounts ?? EmptyCounts(),
                document?.UnknownLevelCounts ?? EmptyCounts(),
                document?.DurationBandCounts ?? EmptyCounts(),
                document?.FailureKindCounts ?? EmptyCounts(),
                document?.ComparisonSizeCounts ?? EmptyCounts(),
                document?.QualityIssueCounts ?? EmptyCounts(),
                document?.ZeroResultQualityIssueCounts ?? EmptyCounts(),
                document?.MethodVersionCounts ?? EmptyCounts(),
                reportCount.Total,
                reportCount.Outdated));
            if (dayUtc == toDayUtc)
            {
                break;
            }

            dayUtc = dayUtc.AddDays(1);
        }

        return new ParkFitPilotMetricsSnapshot(daily);
    }

    private async Task<IReadOnlyDictionary<string, (long Total, long Outdated)>> ReadReportCountsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        [
            new BsonDocument("$match", new BsonDocument("submittedAtUtc", new BsonDocument
            {
                ["$gte"] = fromUtc,
                ["$lte"] = toUtc,
            })),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument("$dateToString", new BsonDocument
                {
                    ["format"] = "%Y-%m-%d",
                    ["date"] = "$submittedAtUtc",
                    ["timezone"] = "UTC",
                }),
                ["total"] = new BsonDocument("$sum", 1),
                ["outdated"] = new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$eq", new BsonArray { "$reason", "Outdated" }),
                    1,
                    0,
                })),
            }),
        ];
        List<BsonDocument> documents = await this.reports
            .Aggregate<BsonDocument>(pipeline, new AggregateOptions { MaxTime = QueryTimeout })
            .ToListAsync(cancellationToken);
        return documents
            .Where(static document => document.TryGetValue("_id", out BsonValue? value)
                && value.IsString)
            .ToDictionary(
                static document => document["_id"].AsString,
                static document => (
                    ReadLong(document, "total"),
                    ReadLong(document, "outdated")),
                StringComparer.Ordinal);
    }

    private static void AddOptionalIncrement<TEnum>(
        ICollection<UpdateDefinition<ParkFitPilotDailyMetricsDocument>> updates,
        UpdateDefinitionBuilder<ParkFitPilotDailyMetricsDocument> builder,
        string field,
        TEnum? value)
        where TEnum : struct, Enum
    {
        if (value.HasValue)
        {
            updates.Add(builder.Inc($"{field}.{value.Value}", 1L));
        }
    }

    private static Dictionary<string, long> EmptyCounts()
    {
        return new Dictionary<string, long>(StringComparer.Ordinal);
    }

    private static string NormalizeCounterKey(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            builder.Append(char.IsAsciiLetterOrDigit(character) || character is '-' or '_'
                ? character
                : '-');
        }

        return builder.ToString();
    }

    private static long ReadLong(BsonDocument document, string field)
    {
        return document.TryGetValue(field, out BsonValue? value) && value.IsNumeric
            ? value.ToInt64()
            : 0L;
    }

    private static string ToDateKey(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
