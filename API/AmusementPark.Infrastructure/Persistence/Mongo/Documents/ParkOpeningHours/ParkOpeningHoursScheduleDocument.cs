using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;

[BsonIgnoreExtraElements]
public sealed class ParkOpeningHoursScheduleDocument : MongoDocumentBase
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("timeZoneId")]
    public string TimeZoneId { get; set; } = string.Empty;

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("notes")]
    [BsonIgnoreIfNull]
    public string? Notes { get; set; }

    [BsonElement("lastVerifiedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastVerifiedAtUtc { get; set; }

    [BsonElement("firstDate")]
    [BsonIgnoreIfNull]
    public string? FirstDate { get; set; }

    [BsonElement("lastDate")]
    [BsonIgnoreIfNull]
    public string? LastDate { get; set; }

    [BsonElement("hasScheduleData")]
    public bool HasScheduleData { get; set; }

    [BsonElement("coverageSegments")]
    public List<ParkOpeningHoursCoverageSegmentDocument> CoverageSegments { get; set; } = new();

    [BsonElement("lastCoverageThirtyDaysNotificationLocalDate")]
    [BsonIgnoreIfNull]
    public string? LastCoverageThirtyDaysNotificationLocalDate { get; set; }

    [BsonElement("lastCoverageExpiredNotificationLocalDate")]
    [BsonIgnoreIfNull]
    public string? LastCoverageExpiredNotificationLocalDate { get; set; }

    [BsonElement("regularRules")]
    public List<ParkOpeningHoursRuleDocument> RegularRules { get; set; } = new();

    [BsonElement("dateOverrides")]
    public List<ParkOpeningHoursDateOverrideDocument> DateOverrides { get; set; } = new();

    [BsonElement("factualRevision")]
    public long FactualRevision { get; set; }

    [BsonElement("writeRevision")]
    public long WriteRevision { get; set; }

    [BsonElement("pendingFactualChanges")]
    public List<FactualChangeOutboxDocument> PendingFactualChanges { get; set; } = new();
}
