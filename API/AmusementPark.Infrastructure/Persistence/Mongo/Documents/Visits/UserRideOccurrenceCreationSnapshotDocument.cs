using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationSnapshotDocument
{
    [BsonElement("visitId")]
    public string VisitId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkItemId")]
    public string ParkItemId { get; set; } = string.Empty;

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }

    [BsonElement("moment")]
    public RideOccurrenceMomentDocument Moment { get; set; } = new RideOccurrenceMomentDocument();

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public RideOccurrenceStatus Status { get; set; }

    [BsonElement("source")]
    [BsonRepresentation(BsonType.String)]
    public RideLogSource Source { get; set; }

    [BsonElement("historicalConsistency")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalConsistency HistoricalConsistency { get; set; }

    [BsonElement("historicalTarget")]
    [BsonIgnoreIfNull]
    public HistoricalTargetReferenceDocument? HistoricalTarget { get; set; }

    [BsonElement("privateNote")]
    [BsonIgnoreIfNull]
    public string? PrivateNote { get; set; }

    [BsonElement("assessment")]
    [BsonIgnoreIfNull]
    public UserRideAssessmentDocument? Assessment { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
