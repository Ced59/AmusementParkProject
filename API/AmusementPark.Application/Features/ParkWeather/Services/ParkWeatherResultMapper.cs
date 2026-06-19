using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Services;

internal static class ParkWeatherResultMapper
{
    public static ParkWeatherRunResult ToResult(this ParkWeatherRun run)
    {
        return new ParkWeatherRunResult
        {
            Id = run.Id ?? string.Empty,
            Trigger = run.Trigger,
            Scope = run.Scope,
            Status = run.Status,
            SourceRunId = run.SourceRunId,
            TargetParkId = run.TargetParkId,
            CancelsAutomaticRunLocalDate = run.CancelsAutomaticRunLocalDate,
            RequestedAtUtc = run.RequestedAtUtc,
            StartedAtUtc = run.StartedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            TotalParkCount = run.TotalParkCount,
            SucceededParkCount = run.SucceededParkCount,
            FailedParkCount = run.FailedParkCount,
            SkippedParkCount = run.SkippedParkCount,
            WarningParkCount = run.WarningParkCount,
            Message = run.Message,
        };
    }

    public static ParkWeatherRunItemResult ToResult(this ParkWeatherRunItem item)
    {
        return new ParkWeatherRunItemResult
        {
            Id = item.Id ?? string.Empty,
            RunId = item.RunId,
            ParkId = item.ParkId,
            ParkName = item.ParkName,
            Status = item.Status,
            AttemptCount = item.AttemptCount,
            StartedAtUtc = item.StartedAtUtc,
            CompletedAtUtc = item.CompletedAtUtc,
            ForecastDayCount = item.ForecastDayCount,
            ObservationDayCount = item.ObservationDayCount,
            WarningMessage = item.WarningMessage,
            ErrorCode = item.ErrorCode,
            ErrorMessage = item.ErrorMessage,
        };
    }

    public static ParkWeatherDailyForecastResult ToForecastResult(this ParkWeatherDailySnapshot snapshot)
    {
        return new ParkWeatherDailyForecastResult
        {
            LocalDate = snapshot.LocalDate,
            DataKind = snapshot.DataKind,
            WeatherCode = snapshot.WeatherCode,
            TemperatureMinCelsius = snapshot.TemperatureMinCelsius,
            TemperatureMaxCelsius = snapshot.TemperatureMaxCelsius,
            ApparentTemperatureMinCelsius = snapshot.ApparentTemperatureMinCelsius,
            ApparentTemperatureMaxCelsius = snapshot.ApparentTemperatureMaxCelsius,
            PrecipitationProbabilityMaxPercent = snapshot.PrecipitationProbabilityMaxPercent,
            PrecipitationSumMillimeters = snapshot.PrecipitationSumMillimeters,
            WindSpeedMaxKilometersPerHour = snapshot.WindSpeedMaxKilometersPerHour,
            WindGustsMaxKilometersPerHour = snapshot.WindGustsMaxKilometersPerHour,
            TimeZone = snapshot.TimeZone,
            FetchedAtUtc = snapshot.FetchedAtUtc,
        };
    }
}
