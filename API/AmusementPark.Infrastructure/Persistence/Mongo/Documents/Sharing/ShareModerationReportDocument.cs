using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class ShareModerationReportDocument : MongoDocumentBase
{
    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public ShareModerationTargetType TargetType { get; set; }

    [BsonElement("targetRecordId")]
    public string TargetRecordId { get; set; } = string.Empty;

    [BsonElement("reason")]
    [BsonRepresentation(BsonType.String)]
    public ShareModerationReason Reason { get; set; }

    [BsonElement("details")]
    [BsonIgnoreIfNull]
    public string? Details { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ShareModerationReportStatus Status { get; set; }

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

    [BsonElement("version")]
    public long Version { get; set; }
}
