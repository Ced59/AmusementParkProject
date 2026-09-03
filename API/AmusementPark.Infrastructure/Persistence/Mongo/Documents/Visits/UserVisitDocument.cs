using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserVisitDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("date")]
    public VisitDateDocument Date { get; set; } = new VisitDateDocument();

    [BsonElement("dateSortKey")]
    public int DateSortKey { get; set; }

    [BsonElement("timeZoneId")]
    [BsonIgnoreIfNull]
    public string? TimeZoneId { get; set; }

    [BsonElement("serviceDayConvention")]
    [BsonRepresentation(BsonType.String)]
    public LocalServiceDayConvention ServiceDayConvention { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public VisitStatus Status { get; set; }

    [BsonElement("privacy")]
    [BsonRepresentation(BsonType.String)]
    public VisitPrivacy Privacy { get; set; }

    [BsonElement("title")]
    [BsonIgnoreIfNull]
    public string? Title { get; set; }

    [BsonElement("privateNote")]
    [BsonIgnoreIfNull]
    public string? PrivateNote { get; set; }

    [BsonElement("parkAssessment")]
    [BsonIgnoreIfNull]
    public UserVisitParkAssessmentDocument? ParkAssessment { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("creationOperationKeyHash")]
    [BsonIgnoreIfNull]
    public string? CreationOperationKeyHash { get; set; }

    [BsonElement("creationPayloadHash")]
    [BsonIgnoreIfNull]
    public string? CreationPayloadHash { get; set; }

    [BsonElement("creationSnapshot")]
    [BsonIgnoreIfNull]
    public UserVisitCreationSnapshotDocument? CreationSnapshot { get; set; }

    [BsonElement("pendingAuditEvents")]
    [BsonIgnoreIfNull]
    public List<PassportAuditEventDocument>? PendingAuditEvents { get; set; }

    [BsonElement("contentMutationLeaseToken")]
    [BsonIgnoreIfNull]
    public string? ContentMutationLeaseToken { get; set; }

    [BsonElement("contentMutationLeaseExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ContentMutationLeaseExpiresAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserVisitParkAssessmentDocument
{
    [BsonElement("valueHalfSteps")]
    public byte ValueHalfSteps { get; set; }

    [BsonElement("privateComment")]
    [BsonIgnoreIfNull]
    public string? PrivateComment { get; set; }

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserVisitCreationSnapshotDocument
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("date")]
    public VisitDateDocument Date { get; set; } = new VisitDateDocument();

    [BsonElement("timeZoneId")]
    [BsonIgnoreIfNull]
    public string? TimeZoneId { get; set; }

    [BsonElement("serviceDayConvention")]
    [BsonRepresentation(BsonType.String)]
    public LocalServiceDayConvention ServiceDayConvention { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public VisitStatus Status { get; set; }

    [BsonElement("privacy")]
    [BsonRepresentation(BsonType.String)]
    public VisitPrivacy Privacy { get; set; }

    [BsonElement("title")]
    [BsonIgnoreIfNull]
    public string? Title { get; set; }

    [BsonElement("privateNote")]
    [BsonIgnoreIfNull]
    public string? PrivateNote { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class VisitDateDocument
{
    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("month")]
    [BsonIgnoreIfNull]
    public int? Month { get; set; }

    [BsonElement("day")]
    [BsonIgnoreIfNull]
    public int? Day { get; set; }

    [BsonElement("precision")]
    [BsonRepresentation(BsonType.String)]
    public VisitDatePrecision Precision { get; set; }

    [BsonElement("isApproximate")]
    public bool IsApproximate { get; set; }
}
