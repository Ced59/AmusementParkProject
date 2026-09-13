using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;

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
