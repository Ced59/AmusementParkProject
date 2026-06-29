using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
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
}

[BsonIgnoreExtraElements]
public sealed class ParkOpeningHoursCoverageSegmentDocument
{
    [BsonElement("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [BsonElement("endDate")]
    public string EndDate { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public sealed class ParkOpeningHoursRuleDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [BsonElement("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [BsonElement("daysOfWeek")]
    public List<string> DaysOfWeek { get; set; } = new();

    [BsonElement("isClosed")]
    public bool IsClosed { get; set; }

    [BsonElement("label")]
    [BsonIgnoreIfNull]
    public string? Label { get; set; }

    [BsonElement("reason")]
    [BsonIgnoreIfNull]
    public string? Reason { get; set; }

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("timeRanges")]
    public List<ParkOpeningHoursTimeRangeDocument> TimeRanges { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class ParkOpeningHoursDateOverrideDocument
{
    [BsonElement("localDate")]
    public string LocalDate { get; set; } = string.Empty;

    [BsonElement("isClosed")]
    public bool IsClosed { get; set; }

    [BsonElement("label")]
    [BsonIgnoreIfNull]
    public string? Label { get; set; }

    [BsonElement("reason")]
    [BsonIgnoreIfNull]
    public string? Reason { get; set; }

    [BsonElement("timeRanges")]
    public List<ParkOpeningHoursTimeRangeDocument> TimeRanges { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class ParkOpeningHoursTimeRangeDocument
{
    [BsonElement("opensAt")]
    public string OpensAt { get; set; } = string.Empty;

    [BsonElement("closesAt")]
    public string ClosesAt { get; set; } = string.Empty;

    [BsonElement("closesNextDay")]
    public bool ClosesNextDay { get; set; }

    [BsonElement("lastAdmissionAt")]
    [BsonIgnoreIfNull]
    public string? LastAdmissionAt { get; set; }

    [BsonElement("lastAdmissionNextDay")]
    public bool LastAdmissionNextDay { get; set; }
}
