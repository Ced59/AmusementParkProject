using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;

public sealed class ParkWeatherDailySnapshotDocument : MongoDocumentBase
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("localDate")]
    public string LocalDate { get; set; } = string.Empty;

    [BsonElement("dataKind")]
    [BsonRepresentation(BsonType.String)]
    public ParkWeatherDataKind DataKind { get; set; }

    [BsonElement("sourceProvider")]
    public string SourceProvider { get; set; } = string.Empty;

    [BsonElement("fetchedAtUtc")]
    public DateTime FetchedAtUtc { get; set; }

    [BsonElement("providerGeneratedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ProviderGeneratedAtUtc { get; set; }

    [BsonElement("timeZone")]
    [BsonIgnoreIfNull]
    public string? TimeZone { get; set; }

    [BsonElement("utcOffsetSeconds")]
    [BsonIgnoreIfNull]
    public int? UtcOffsetSeconds { get; set; }

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("weatherCode")]
    [BsonIgnoreIfNull]
    public int? WeatherCode { get; set; }

    [BsonElement("temperatureMinCelsius")]
    [BsonIgnoreIfNull]
    public double? TemperatureMinCelsius { get; set; }

    [BsonElement("temperatureMaxCelsius")]
    [BsonIgnoreIfNull]
    public double? TemperatureMaxCelsius { get; set; }

    [BsonElement("apparentTemperatureMinCelsius")]
    [BsonIgnoreIfNull]
    public double? ApparentTemperatureMinCelsius { get; set; }

    [BsonElement("apparentTemperatureMaxCelsius")]
    [BsonIgnoreIfNull]
    public double? ApparentTemperatureMaxCelsius { get; set; }

    [BsonElement("precipitationProbabilityMaxPercent")]
    [BsonIgnoreIfNull]
    public int? PrecipitationProbabilityMaxPercent { get; set; }

    [BsonElement("precipitationSumMillimeters")]
    [BsonIgnoreIfNull]
    public double? PrecipitationSumMillimeters { get; set; }

    [BsonElement("windSpeedMaxKilometersPerHour")]
    [BsonIgnoreIfNull]
    public double? WindSpeedMaxKilometersPerHour { get; set; }

    [BsonElement("windGustsMaxKilometersPerHour")]
    [BsonIgnoreIfNull]
    public double? WindGustsMaxKilometersPerHour { get; set; }
}

public sealed class ParkWeatherRunDocument : MongoDocumentBase
{
    [BsonElement("trigger")]
    [BsonRepresentation(BsonType.String)]
    public ParkWeatherRunTrigger Trigger { get; set; }

    [BsonElement("scope")]
    [BsonRepresentation(BsonType.String)]
    public ParkWeatherRefreshScope Scope { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ParkWeatherRunStatus Status { get; set; }

    [BsonElement("sourceRunId")]
    [BsonIgnoreIfNull]
    public string? SourceRunId { get; set; }

    [BsonElement("targetParkId")]
    [BsonIgnoreIfNull]
    public string? TargetParkId { get; set; }

    [BsonElement("cancelsAutomaticRunLocalDate")]
    [BsonIgnoreIfNull]
    public string? CancelsAutomaticRunLocalDate { get; set; }

    [BsonElement("requestedAtUtc")]
    public DateTime RequestedAtUtc { get; set; }

    [BsonElement("startedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? StartedAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("totalParkCount")]
    public int TotalParkCount { get; set; }

    [BsonElement("succeededParkCount")]
    public int SucceededParkCount { get; set; }

    [BsonElement("failedParkCount")]
    public int FailedParkCount { get; set; }

    [BsonElement("skippedParkCount")]
    public int SkippedParkCount { get; set; }

    [BsonElement("warningParkCount")]
    public int WarningParkCount { get; set; }

    [BsonElement("message")]
    [BsonIgnoreIfNull]
    public string? Message { get; set; }
}

public sealed class ParkWeatherRunItemDocument : MongoDocumentBase
{
    [BsonElement("runId")]
    public string RunId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkName")]
    [BsonIgnoreIfNull]
    public string? ParkName { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ParkWeatherRunItemStatus Status { get; set; }

    [BsonElement("attemptCount")]
    public int AttemptCount { get; set; }

    [BsonElement("startedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? StartedAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("forecastDayCount")]
    public int ForecastDayCount { get; set; }

    [BsonElement("observationDayCount")]
    public int ObservationDayCount { get; set; }

    [BsonElement("warningMessage")]
    [BsonIgnoreIfNull]
    public string? WarningMessage { get; set; }

    [BsonElement("errorCode")]
    [BsonIgnoreIfNull]
    public string? ErrorCode { get; set; }

    [BsonElement("errorMessage")]
    [BsonIgnoreIfNull]
    public string? ErrorMessage { get; set; }
}
