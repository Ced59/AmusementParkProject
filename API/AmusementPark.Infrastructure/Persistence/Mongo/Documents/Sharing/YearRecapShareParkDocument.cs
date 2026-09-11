using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class YearRecapShareParkDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("visitCount")]
    public long VisitCount { get; set; }

    [BsonElement("completedRideCount")]
    [BsonIgnoreIfNull]
    public long? CompletedRideCount { get; set; }
}
