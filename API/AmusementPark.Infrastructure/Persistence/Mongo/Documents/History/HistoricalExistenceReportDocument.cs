using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

[BsonIgnoreExtraElements]
public sealed class HistoricalExistenceReportDocument : MongoDocumentBase
{
    [BsonElement("ownerUserId")]
    public string OwnerUserId { get; set; } = string.Empty;

    [BsonElement("visitId")]
    public string VisitId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkName")]
    public string ParkName { get; set; } = string.Empty;

    [BsonElement("visitDate")]
    public VisitDateDocument VisitDate { get; set; } = new();

    [BsonElement("claimedName")]
    public string ClaimedName { get; set; } = string.Empty;

    [BsonElement("normalizedClaimedName")]
    public string NormalizedClaimedName { get; set; } = string.Empty;

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("sourceReference")]
    [BsonIgnoreIfNull]
    public string? SourceReference { get; set; }

    [BsonElement("details")]
    [BsonIgnoreIfNull]
    public string? Details { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalExistenceReportStatus Status { get; set; }

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
