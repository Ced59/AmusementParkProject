using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

[BsonIgnoreExtraElements]
public sealed class ParkFitSourceReportDocument : MongoDocumentBase
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkName")]
    public string ParkName { get; set; } = string.Empty;

    [BsonElement("evidenceKind")]
    [BsonRepresentation(BsonType.String)]
    public ParkFitEvidenceKind EvidenceKind { get; set; }

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("sourceReference")]
    [BsonIgnoreIfNull]
    public string? SourceReference { get; set; }

    [BsonElement("reason")]
    [BsonRepresentation(BsonType.String)]
    public ParkFitSourceReportReason Reason { get; set; }

    [BsonElement("details")]
    [BsonIgnoreIfNull]
    public string? Details { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ParkFitSourceReportStatus Status { get; set; }

    [BsonElement("submittedAtUtc")]
    public DateTime SubmittedAtUtc { get; set; }

    [BsonElement("reviewedByUserId")]
    [BsonIgnoreIfNull]
    public string? ReviewedByUserId { get; set; }

    [BsonElement("reviewedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ReviewedAtUtc { get; set; }

    [BsonElement("decisionNote")]
    [BsonIgnoreIfNull]
    public string? DecisionNote { get; set; }

    [BsonElement("revision")]
    public long Revision { get; set; }
}
