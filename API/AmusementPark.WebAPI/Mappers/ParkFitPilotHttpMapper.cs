using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkFit;

namespace AmusementPark.WebAPI.Mappers;

public static class ParkFitPilotHttpMapper
{
    public static bool TryToCommand(
        this CaptureParkFitPilotObservationRequestDto request,
        out CaptureParkFitPilotObservationCommand? command)
    {
        command = null;
        if (request.QualityIssues?.Count > 16
            || !TryParseRequired(request.EventKind, out ParkFitPilotEventKind eventKind)
            || !TryParseOptional(request.ResultBand, out ParkFitPilotResultBand? resultBand)
            || !TryParseOptional(request.UnknownLevel, out ParkFitPilotUnknownLevel? unknownLevel)
            || !TryParseOptional(request.DurationBand, out ParkFitPilotDurationBand? durationBand)
            || !TryParseOptional(request.FailureKind, out ParkFitPilotFailureKind? failureKind)
            || !TryParseOptional(request.ComparisonSize, out ParkFitPilotComparisonSize? comparisonSize)
            || !TryParseAll(
                request.QualityIssues ?? Array.Empty<string>(),
                out IReadOnlyCollection<ParkFitDataQualityIssue> issues))
        {
            return false;
        }

        command = new CaptureParkFitPilotObservationCommand(
            eventKind,
            resultBand,
            unknownLevel,
            durationBand,
            failureKind,
            comparisonSize,
            request.MethodVersion,
            issues);
        return true;
    }

    public static ParkFitPilotMetricsDto ToHttp(this ParkFitPilotMetricsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ParkFitPilotMetricsDto
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            FromUtc = result.FromUtc,
            ToUtc = result.ToUtc,
            SearchesStarted = result.SearchesStarted,
            SearchesCompleted = result.SearchesCompleted,
            SearchesFailed = result.SearchesFailed,
            SearchesAbandoned = result.SearchesAbandoned,
            ExplanationsViewed = result.ExplanationsViewed,
            ComparisonsOpened = result.ComparisonsOpened,
            SourceReports = result.SourceReports,
            OutdatedSourceReports = result.OutdatedSourceReports,
            Health = new ParkFitPilotHealthDto
            {
                CompletionRatePercent = result.Health.CompletionRatePercent,
                NoResultRatePercent = result.Health.NoResultRatePercent,
                SignificantUnknownRatePercent = result.Health.SignificantUnknownRatePercent,
                ExplanationOpenRatePercent = result.Health.ExplanationOpenRatePercent,
                ComparisonOpenRatePercent = result.Health.ComparisonOpenRatePercent,
                Signal = result.Health.Signal.ToString(),
                RequiresQualitativeReview = result.Health.RequiresQualitativeReview,
            },
            ResultBandCounts = result.ResultBandCounts,
            UnknownLevelCounts = result.UnknownLevelCounts,
            DurationBandCounts = result.DurationBandCounts,
            FailureKindCounts = result.FailureKindCounts,
            ComparisonSizeCounts = result.ComparisonSizeCounts,
            QualityIssueCounts = result.QualityIssueCounts,
            ZeroResultQualityIssueCounts = result.ZeroResultQualityIssueCounts,
            MethodVersionCounts = result.MethodVersionCounts,
            Daily = result.Daily.Select(ToHttp).ToList(),
        };
    }

    private static ParkFitPilotDailyMetricsDto ToHttp(ParkFitPilotDailyMetrics metrics)
    {
        return new ParkFitPilotDailyMetricsDto
        {
            Date = metrics.Date,
            EventCounts = metrics.EventCounts,
            SourceReports = metrics.SourceReports,
            OutdatedSourceReports = metrics.OutdatedSourceReports,
        };
    }

    private static bool TryParseRequired<TEnum>(string value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: false, out parsed)
            && Enum.IsDefined(parsed)
            && string.Equals(value, parsed.ToString(), StringComparison.Ordinal);
    }

    private static bool TryParseOptional<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!TryParseRequired(value, out TEnum enumValue))
        {
            return false;
        }

        parsed = enumValue;
        return true;
    }

    private static bool TryParseAll<TEnum>(
        IEnumerable<string> values,
        out IReadOnlyCollection<TEnum> parsed)
        where TEnum : struct, Enum
    {
        List<TEnum> result = new();
        foreach (string value in values)
        {
            if (!TryParseRequired(value, out TEnum item))
            {
                parsed = Array.Empty<TEnum>();
                return false;
            }

            result.Add(item);
        }

        parsed = result;
        return true;
    }
}
