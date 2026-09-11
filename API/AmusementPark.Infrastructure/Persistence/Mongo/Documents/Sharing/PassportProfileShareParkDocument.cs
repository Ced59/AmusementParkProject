using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareParkDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("visitCount")]
    public long VisitCount { get; set; }

    [BsonElement("firstVisitYear")]
    public int FirstVisitYear { get; set; }

    [BsonElement("lastVisitYear")]
    public int LastVisitYear { get; set; }

    [BsonElement("completedRideCount")]
    [BsonIgnoreIfNull]
    public long? CompletedRideCount { get; set; }

    [BsonElement("visitRatings")]
    [BsonIgnoreIfNull]
    public PassportProfileShareRatingSummaryDocument? VisitRatings { get; set; }
}
