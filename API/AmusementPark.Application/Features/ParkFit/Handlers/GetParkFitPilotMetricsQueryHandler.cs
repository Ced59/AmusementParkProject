using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class GetParkFitPilotMetricsQueryHandler
    : IQueryHandler<GetParkFitPilotMetricsQuery, ApplicationResult<ParkFitPilotMetricsResult>>
{
    private const int DefaultRangeDays = 30;
    private const int MaximumRangeDays = 180;

    private readonly IParkFitPilotMetricsRepository repository;
    private readonly TimeProvider timeProvider;

    public GetParkFitPilotMetricsQueryHandler(
        IParkFitPilotMetricsRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ParkFitPilotMetricsResult>> HandleAsync(
        GetParkFitPilotMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        DateTime generatedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime requestedToUtc = NormalizeUtc(query.ToUtc) ?? generatedAtUtc;
        DateTime? requestedFromUtc = NormalizeUtc(query.FromUtc);
        if (requestedFromUtc.HasValue && requestedFromUtc.Value > requestedToUtc)
        {
            return ApplicationResult<ParkFitPilotMetricsResult>.Failure(
                ParkFitPilotApplicationErrors.InvalidMetricsRange());
        }

        DateTime toDayUtc = StartOfUtcDay(requestedToUtc);
        DateTime toUtc = EndOfUtcDay(toDayUtc);
        DateTime defaultFromUtc = SubtractWholeDaysOrMinimum(toDayUtc, DefaultRangeDays - 1);
        DateTime fromUtc = requestedFromUtc.HasValue
            ? StartOfUtcDay(requestedFromUtc.Value)
            : defaultFromUtc;
        DateTime earliestAllowedUtc = SubtractWholeDaysOrMinimum(
            toDayUtc,
            MaximumRangeDays - 1);
        if (fromUtc < earliestAllowedUtc)
        {
            fromUtc = earliestAllowedUtc;
        }

        ParkFitPilotMetricsSnapshot snapshot = await this.repository.ReadAsync(
            fromUtc,
            toUtc,
            cancellationToken);
        IReadOnlyDictionary<string, long> eventCounts = Sum(
            snapshot.Daily,
            static day => day.EventCounts);
        IReadOnlyDictionary<string, long> resultBandCounts = Sum(
            snapshot.Daily,
            static day => day.ResultBandCounts);
        IReadOnlyDictionary<string, long> unknownLevelCounts = Sum(
            snapshot.Daily,
            static day => day.UnknownLevelCounts);
        long searchesStarted = Count(eventCounts, ParkFitPilotEventKind.SearchStarted);
        long searchesCompleted = Count(eventCounts, ParkFitPilotEventKind.SearchCompleted);
        long searchesFailed = Count(eventCounts, ParkFitPilotEventKind.SearchFailed);
        ParkFitPilotHealth health = ParkFitPilotHealthEvaluator.Evaluate(
            searchesStarted,
            searchesCompleted,
            Count(resultBandCounts, ParkFitPilotResultBand.None),
            Count(unknownLevelCounts, ParkFitPilotUnknownLevel.Significant),
            Count(eventCounts, ParkFitPilotEventKind.ExplanationViewed),
            Count(eventCounts, ParkFitPilotEventKind.ComparisonOpened));

        ParkFitPilotMetricsResult result = new(
            generatedAtUtc,
            fromUtc,
            toUtc,
            searchesStarted,
            searchesCompleted,
            searchesFailed,
            Math.Max(0L, searchesStarted - searchesCompleted - searchesFailed),
            Count(eventCounts, ParkFitPilotEventKind.ExplanationViewed),
            Count(eventCounts, ParkFitPilotEventKind.ComparisonOpened),
            snapshot.Daily.Sum(static day => day.SourceReports),
            snapshot.Daily.Sum(static day => day.OutdatedSourceReports),
            health,
            resultBandCounts,
            unknownLevelCounts,
            Sum(snapshot.Daily, static day => day.DurationBandCounts),
            Sum(snapshot.Daily, static day => day.FailureKindCounts),
            Sum(snapshot.Daily, static day => day.ComparisonSizeCounts),
            Sum(snapshot.Daily, static day => day.QualityIssueCounts),
            Sum(snapshot.Daily, static day => day.ZeroResultQualityIssueCounts),
            Sum(snapshot.Daily, static day => day.MethodVersionCounts),
            snapshot.Daily);
        return ApplicationResult<ParkFitPilotMetricsResult>.Success(result);
    }

    private static IReadOnlyDictionary<string, long> Sum(
        IEnumerable<ParkFitPilotDailyMetrics> daily,
        Func<ParkFitPilotDailyMetrics, IReadOnlyDictionary<string, long>> selector)
    {
        return daily
            .SelectMany(day => selector(day))
            .GroupBy(static item => item.Key, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Sum(static item => item.Value),
                StringComparer.Ordinal);
    }

    private static long Count<TEnum>(IReadOnlyDictionary<string, long> counts, TEnum key)
        where TEnum : struct, Enum
    {
        return counts.GetValueOrDefault(key.ToString());
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : value.Value.ToUniversalTime();
    }

    private static DateTime StartOfUtcDay(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    private static DateTime EndOfUtcDay(DateTime startOfDayUtc)
    {
        return startOfDayUtc == DateTime.SpecifyKind(DateTime.MaxValue.Date, DateTimeKind.Utc)
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : startOfDayUtc.AddDays(1).AddTicks(-1);
    }

    private static DateTime SubtractWholeDaysOrMinimum(DateTime value, int days)
    {
        DateTime minimumUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        return value < minimumUtc.AddDays(days)
            ? minimumUtc
            : value.AddDays(-days);
    }
}
