using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;

[BsonIgnoreExtraElements]
public sealed class ParkOpeningHoursDateOverrideDocument
{
    [BsonElement("localDate")]
    public string LocalDate { get; set; } = string.Empty;

    [BsonElement("isClosed")]
    public bool IsClosed { get; set; }

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("reasons")]
    public List<LocalizedTextDocument> Reasons { get; set; } = new();

    [BsonElement("timeRanges")]
    public List<ParkOpeningHoursTimeRangeDocument> TimeRanges { get; set; } = new();
}
