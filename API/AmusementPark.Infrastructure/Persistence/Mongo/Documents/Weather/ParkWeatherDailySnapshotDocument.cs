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
