using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;

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
