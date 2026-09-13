using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;

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

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("reasons")]
    public List<LocalizedTextDocument> Reasons { get; set; } = new();

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("timeRanges")]
    public List<ParkOpeningHoursTimeRangeDocument> TimeRanges { get; set; } = new();
}
