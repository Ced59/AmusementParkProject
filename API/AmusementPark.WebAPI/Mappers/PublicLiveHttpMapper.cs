using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.WebAPI.Contracts.LiveData;

namespace AmusementPark.WebAPI.Mappers;

public static class PublicLiveHttpMapper
{
    public static PublicLiveForecastDto ToHttp(this PublicLiveForecastResult result)
    {
        return new PublicLiveForecastDto(
            result.TargetDisplayName,
            result.ParkDisplayName,
            result.TimeZoneId,
            result.Forecast.ForecastFromUtc,
            result.Forecast.ForecastToUtc,
            result.Forecast.CalculatedAtUtc,
            result.Forecast.ExpectedWaitMinutes,
            result.Forecast.LowerBoundMinutes,
            result.Forecast.UpperBoundMinutes,
            result.Forecast.TrainingDayCount,
            result.StudyVersion,
            result.Method,
            result.IntervalMethod,
            result.MeanAbsoluteErrorMinutes,
            result.IntervalCoveragePercent,
            result.EvaluationFromUtc,
            result.EvaluationToUtc,
            result.EvaluationPointCount,
            new PublicLiveSourceDto(
                result.Source.Id,
                result.Source.DisplayName,
                result.Source.Type.ToString(),
                result.Source.AttributionText,
                result.Source.AttributionUrl));
    }

    public static PublicLiveHistoryDto ToHttp(this PublicLiveHistoryResult result)
    {
        return new PublicLiveHistoryDto(
            result.TargetId,
            result.DisplayName,
            result.ParkId,
            result.ParkDisplayName,
            result.FromUtc,
            result.ToUtc,
            result.TimeZoneId,
            result.DataStatus.ToString(),
            result.ExpectedObservationCount,
            result.ObservationCount,
            result.UsableWaitCount,
            result.DaysCovered,
            result.ComparableDays,
            result.CoveragePercent,
            result.TruncatedObservationCount,
            new PublicLiveHistoryExclusionsDto(
                result.Exclusions.DuplicateObservations,
                result.Exclusions.OutsideActiveWindow,
                result.Exclusions.NonOperatingStatus,
                result.Exclusions.MissingStandbyWait,
                result.Exclusions.Total),
            result.Hours.Select(static hour => new PublicLiveHistoryHourDto(
                hour.LocalHour,
                hour.DataStatus.ToString(),
                hour.ExpectedObservationCount,
                hour.ObservationCount,
                hour.UsableWaitCount,
                hour.DaysCovered,
                hour.ComparableDays,
                hour.CoveragePercent,
                hour.RobustMinimumMinutes,
                hour.FirstQuartileMinutes,
                hour.MedianMinutes,
                hour.ThirdQuartileMinutes,
                hour.RobustMaximumMinutes)).ToList().AsReadOnly(),
            new PublicLiveSourceDto(
                result.Source.Id,
                result.Source.DisplayName,
                result.Source.Type.ToString(),
                result.Source.AttributionText,
                result.Source.AttributionUrl));
    }

    public static PublicLiveTargetDto ToHttp(this PublicLiveTargetResult result)
    {
        return new PublicLiveTargetDto(
            result.TargetId,
            result.TargetType.ToString(),
            result.DisplayName,
            result.ParkId,
            result.ParkDisplayName,
            result.Availability.ToString(),
            result.Status?.ToString(),
            result.Queues.Select(static queue => new PublicLiveQueueDto(
                queue.Kind.ToString(),
                queue.WaitTimeMinutes,
                queue.IsEstimated,
                queue.Availability.ToString(),
                queue.ReturnStartUtc,
                queue.ReturnEndUtc,
                queue.CurrentGroupStart,
                queue.CurrentGroupEnd,
                queue.NextAllocationUtc,
                queue.PriceMinorUnits,
                queue.CurrencyCode)).ToList().AsReadOnly(),
            result.AsOfUtc,
            result.ObservedAtUtc,
            result.ReceivedAtUtc,
            result.AgeSeconds,
            result.Freshness?.ToString(),
            result.ExpiresAtUtc,
            result.Source is null
                ? null
                : new PublicLiveSourceDto(
                    result.Source.Id,
                    result.Source.DisplayName,
                    result.Source.Type.ToString(),
                    result.Source.AttributionText,
                    result.Source.AttributionUrl),
            result.Confidence?.ToString());
    }

    public static PublicParkLiveItemsDto ToHttp(this PublicParkLiveItemsResult result)
    {
        return new PublicParkLiveItemsDto(
            result.ParkId,
            result.ParkDisplayName,
            result.AsOfUtc,
            result.Items.Select(static item => item.ToHttp()).ToList().AsReadOnly());
    }
}
