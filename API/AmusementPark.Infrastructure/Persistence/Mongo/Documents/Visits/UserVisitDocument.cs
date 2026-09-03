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
