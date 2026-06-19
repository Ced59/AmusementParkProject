using System.Globalization;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.WebAPI.Contracts.ParkWeather;

namespace AmusementPark.WebAPI.Mappers;

internal static class ParkWeatherHttpMappers
{
    public static ParkWeatherForecastDto ToHttp(this ParkWeatherForecastResult result)
    {
        return new ParkWeatherForecastDto
        {
            ParkId = result.ParkId,
            Days = result.Days.Select(static day => day.ToHttp()).ToList(),
            Attribution = result.Attribution.ToHttp(),
        };
    }

    public static ParkWeatherHistoricalComparisonsDto ToHttp(this ParkWeatherHistoricalComparisonsResult result)
    {
        return new ParkWeatherHistoricalComparisonsDto
        {
            ParkId = result.ParkId,
            Years = result.Years.Select(static year => year.ToHttp()).ToList(),
            Attribution = result.Attribution.ToHttp(),
        };
    }

    public static ParkWeatherHistoricalComparisonDto ToHttp(this ParkWeatherHistoricalComparisonResult result)
    {
        return new ParkWeatherHistoricalComparisonDto
        {
            YearsBack = result.YearsBack,
            Days = result.Days.Select(static day => day.ToHttp()).ToList(),
        };
    }

    public static ParkWeatherHistoricalComparisonDayDto ToHttp(this ParkWeatherHistoricalComparisonDayResult result)
    {
        return new ParkWeatherHistoricalComparisonDayDto
        {
            ForecastLocalDate = result.ForecastLocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            LocalDate = result.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            WeatherCode = result.WeatherCode,
            TemperatureMinCelsius = result.TemperatureMinCelsius,
            TemperatureMaxCelsius = result.TemperatureMaxCelsius,
            ApparentTemperatureMinCelsius = result.ApparentTemperatureMinCelsius,
            ApparentTemperatureMaxCelsius = result.ApparentTemperatureMaxCelsius,
            PrecipitationSumMillimeters = result.PrecipitationSumMillimeters,
            WindSpeedMaxKilometersPerHour = result.WindSpeedMaxKilometersPerHour,
            WindGustsMaxKilometersPerHour = result.WindGustsMaxKilometersPerHour,
        };
    }

    public static ParkWeatherDailyForecastDto ToHttp(this ParkWeatherDailyForecastResult result)
    {
        return new ParkWeatherDailyForecastDto
        {
            LocalDate = result.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DataKind = result.DataKind.ToString(),
            WeatherCode = result.WeatherCode,
            TemperatureMinCelsius = result.TemperatureMinCelsius,
            TemperatureMaxCelsius = result.TemperatureMaxCelsius,
            ApparentTemperatureMinCelsius = result.ApparentTemperatureMinCelsius,
            ApparentTemperatureMaxCelsius = result.ApparentTemperatureMaxCelsius,
            PrecipitationProbabilityMaxPercent = result.PrecipitationProbabilityMaxPercent,
            PrecipitationSumMillimeters = result.PrecipitationSumMillimeters,
            WindSpeedMaxKilometersPerHour = result.WindSpeedMaxKilometersPerHour,
            WindGustsMaxKilometersPerHour = result.WindGustsMaxKilometersPerHour,
            TimeZone = result.TimeZone,
            FetchedAtUtc = result.FetchedAtUtc,
        };
    }

    public static ParkWeatherAttributionDto ToHttp(this ParkWeatherAttributionResult result)
    {
        return new ParkWeatherAttributionDto
        {
            ProviderName = result.ProviderName,
            ProviderUrl = result.ProviderUrl,
            LicenseName = result.LicenseName,
            LicenseUrl = result.LicenseUrl,
        };
    }

    public static ParkWeatherRunDto ToHttp(this ParkWeatherRunResult result)
    {
        return new ParkWeatherRunDto
        {
            Id = result.Id,
            Trigger = result.Trigger.ToString(),
            Scope = result.Scope.ToString(),
            Status = result.Status.ToString(),
            SourceRunId = result.SourceRunId,
            TargetParkId = result.TargetParkId,
            CancelsAutomaticRunLocalDate = result.CancelsAutomaticRunLocalDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            RequestedAtUtc = result.RequestedAtUtc,
            StartedAtUtc = result.StartedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
            TotalParkCount = result.TotalParkCount,
            SucceededParkCount = result.SucceededParkCount,
            FailedParkCount = result.FailedParkCount,
            SkippedParkCount = result.SkippedParkCount,
            WarningParkCount = result.WarningParkCount,
            Message = result.Message,
        };
    }

    public static ParkWeatherRunItemDto ToHttp(this ParkWeatherRunItemResult result)
    {
        return new ParkWeatherRunItemDto
        {
            Id = result.Id,
            RunId = result.RunId,
            ParkId = result.ParkId,
            ParkName = result.ParkName,
            Status = result.Status.ToString(),
            AttemptCount = result.AttemptCount,
            StartedAtUtc = result.StartedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
            ForecastDayCount = result.ForecastDayCount,
            ObservationDayCount = result.ObservationDayCount,
            WarningMessage = result.WarningMessage,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
        };
    }
}
