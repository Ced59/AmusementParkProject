using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class PassportAuditEventDocument
{
    [BsonElement("eventId")]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("entityType")]
    [BsonRepresentation(BsonType.String)]
    public PassportAuditEntityType EntityType { get; set; }

    [BsonElement("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [BsonElement("visitId")]
    public string VisitId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkItemId")]
    [BsonIgnoreIfNull]
    public string? ParkItemId { get; set; }

    [BsonElement("eventType")]
    [BsonRepresentation(BsonType.String)]
    public PassportAuditEventType EventType { get; set; }

    [BsonElement("entityVersion")]
    public long EntityVersion { get; set; }

    [BsonElement("assessmentRevision")]
    [BsonIgnoreIfNull]
    public int? AssessmentRevision { get; set; }

    [BsonElement("changedFields")]
    public List<string> ChangedFields { get; set; } = new List<string>();

    [BsonElement("previousRatingHalfSteps")]
    [BsonIgnoreIfNull]
    public byte? PreviousRatingHalfSteps { get; set; }

    [BsonElement("newRatingHalfSteps")]
    [BsonIgnoreIfNull]
    public byte? NewRatingHalfSteps { get; set; }

    [BsonElement("previousVisitDate")]
    [BsonIgnoreIfNull]
    public VisitDateDocument? PreviousVisitDate { get; set; }

    [BsonElement("newVisitDate")]
    [BsonIgnoreIfNull]
    public VisitDateDocument? NewVisitDate { get; set; }

    [BsonElement("previousVisitStatus")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public VisitStatus? PreviousVisitStatus { get; set; }

    [BsonElement("newVisitStatus")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public VisitStatus? NewVisitStatus { get; set; }

    [BsonElement("previousRideStatus")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public RideOccurrenceStatus? PreviousRideStatus { get; set; }

    [BsonElement("newRideStatus")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public RideOccurrenceStatus? NewRideStatus { get; set; }

    [BsonElement("previousSortPosition")]
    [BsonIgnoreIfNull]
    public long? PreviousSortPosition { get; set; }

    [BsonElement("newSortPosition")]
    [BsonIgnoreIfNull]
    public long? NewSortPosition { get; set; }

    [BsonElement("privateTextChanged")]
    public bool PrivateTextChanged { get; set; }

    [BsonElement("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [BsonElement("origin")]
    [BsonRepresentation(BsonType.String)]
    public PassportAuditOrigin Origin { get; set; }

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class PassportAuditJournalDocument : MongoDocumentBase
{
    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [BsonElement("event")]
    public PassportAuditEventDocument Event { get; set; } = new PassportAuditEventDocument();
}
