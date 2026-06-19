using System.Globalization;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    private const string WeatherDateFormat = "yyyy-MM-dd";

    public static ParkWeatherDailySnapshotDocument ToDocument(this ParkWeatherDailySnapshot snapshot)
    {
        DateTime now = DateTime.UtcNow;
        return new ParkWeatherDailySnapshotDocument
        {
            Id = string.IsNullOrWhiteSpace(snapshot.Id) ? Guid.NewGuid().ToString() : snapshot.Id,
            CreatedAt = now,
            UpdatedAt = now,
            ParkId = snapshot.ParkId,
            LocalDate = FormatDate(snapshot.LocalDate),
            DataKind = snapshot.DataKind,
            SourceProvider = snapshot.SourceProvider,
            FetchedAtUtc = snapshot.FetchedAtUtc,
            ProviderGeneratedAtUtc = snapshot.ProviderGeneratedAtUtc,
            TimeZone = snapshot.TimeZone,
            UtcOffsetSeconds = snapshot.UtcOffsetSeconds,
            Latitude = snapshot.Latitude,
            Longitude = snapshot.Longitude,
            WeatherCode = snapshot.WeatherCode,
            TemperatureMinCelsius = snapshot.TemperatureMinCelsius,
            TemperatureMaxCelsius = snapshot.TemperatureMaxCelsius,
            ApparentTemperatureMinCelsius = snapshot.ApparentTemperatureMinCelsius,
            ApparentTemperatureMaxCelsius = snapshot.ApparentTemperatureMaxCelsius,
            PrecipitationProbabilityMaxPercent = snapshot.PrecipitationProbabilityMaxPercent,
            PrecipitationSumMillimeters = snapshot.PrecipitationSumMillimeters,
            WindSpeedMaxKilometersPerHour = snapshot.WindSpeedMaxKilometersPerHour,
            WindGustsMaxKilometersPerHour = snapshot.WindGustsMaxKilometersPerHour,
        };
    }

    public static ParkWeatherDailySnapshot ToDomain(this ParkWeatherDailySnapshotDocument document)
    {
        return new ParkWeatherDailySnapshot
        {
            Id = document.Id,
            ParkId = document.ParkId,
            LocalDate = ParseDate(document.LocalDate),
            DataKind = document.DataKind,
            SourceProvider = document.SourceProvider,
            FetchedAtUtc = document.FetchedAtUtc,
            ProviderGeneratedAtUtc = document.ProviderGeneratedAtUtc,
            TimeZone = document.TimeZone,
            UtcOffsetSeconds = document.UtcOffsetSeconds,
            Latitude = document.Latitude,
            Longitude = document.Longitude,
            WeatherCode = document.WeatherCode,
            TemperatureMinCelsius = document.TemperatureMinCelsius,
            TemperatureMaxCelsius = document.TemperatureMaxCelsius,
            ApparentTemperatureMinCelsius = document.ApparentTemperatureMinCelsius,
            ApparentTemperatureMaxCelsius = document.ApparentTemperatureMaxCelsius,
            PrecipitationProbabilityMaxPercent = document.PrecipitationProbabilityMaxPercent,
            PrecipitationSumMillimeters = document.PrecipitationSumMillimeters,
            WindSpeedMaxKilometersPerHour = document.WindSpeedMaxKilometersPerHour,
            WindGustsMaxKilometersPerHour = document.WindGustsMaxKilometersPerHour,
        };
    }

    public static ParkWeatherRunDocument ToDocument(this ParkWeatherRun run)
    {
        DateTime now = DateTime.UtcNow;
        return new ParkWeatherRunDocument
        {
            Id = string.IsNullOrWhiteSpace(run.Id) ? Guid.NewGuid().ToString() : run.Id,
            CreatedAt = now,
            UpdatedAt = now,
            Trigger = run.Trigger,
            Scope = run.Scope,
            Status = run.Status,
            SourceRunId = run.SourceRunId,
            TargetParkId = run.TargetParkId,
            CancelsAutomaticRunLocalDate = run.CancelsAutomaticRunLocalDate.HasValue ? FormatDate(run.CancelsAutomaticRunLocalDate.Value) : null,
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

    public static ParkWeatherRun ToDomain(this ParkWeatherRunDocument document)
    {
        return new ParkWeatherRun
        {
            Id = document.Id,
            Trigger = document.Trigger,
            Scope = document.Scope,
            Status = document.Status,
            SourceRunId = document.SourceRunId,
            TargetParkId = document.TargetParkId,
            CancelsAutomaticRunLocalDate = string.IsNullOrWhiteSpace(document.CancelsAutomaticRunLocalDate) ? null : ParseDate(document.CancelsAutomaticRunLocalDate),
            RequestedAtUtc = document.RequestedAtUtc,
            StartedAtUtc = document.StartedAtUtc,
            CompletedAtUtc = document.CompletedAtUtc,
            TotalParkCount = document.TotalParkCount,
            SucceededParkCount = document.SucceededParkCount,
            FailedParkCount = document.FailedParkCount,
            SkippedParkCount = document.SkippedParkCount,
            WarningParkCount = document.WarningParkCount,
            Message = document.Message,
        };
    }

    public static ParkWeatherRunItemDocument ToDocument(this ParkWeatherRunItem item)
    {
        DateTime now = DateTime.UtcNow;
        return new ParkWeatherRunItemDocument
        {
            Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString() : item.Id,
            CreatedAt = now,
            UpdatedAt = now,
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

    public static ParkWeatherRunItem ToDomain(this ParkWeatherRunItemDocument document)
    {
        return new ParkWeatherRunItem
        {
            Id = document.Id,
            RunId = document.RunId,
            ParkId = document.ParkId,
            ParkName = document.ParkName,
            Status = document.Status,
            AttemptCount = document.AttemptCount,
            StartedAtUtc = document.StartedAtUtc,
            CompletedAtUtc = document.CompletedAtUtc,
            ForecastDayCount = document.ForecastDayCount,
            ObservationDayCount = document.ObservationDayCount,
            WarningMessage = document.WarningMessage,
            ErrorCode = document.ErrorCode,
            ErrorMessage = document.ErrorMessage,
        };
    }

    internal static string FormatDate(DateOnly date)
    {
        return date.ToString(WeatherDateFormat, CultureInfo.InvariantCulture);
    }

    internal static DateOnly ParseDate(string date)
    {
        return DateOnly.ParseExact(date, WeatherDateFormat, CultureInfo.InvariantCulture);
    }
}
