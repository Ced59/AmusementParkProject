using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareYearDocument
{
    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("visitCount")]
    public long VisitCount { get; set; }

    [BsonElement("parkCount")]
    public long ParkCount { get; set; }

    [BsonElement("completedRideCount")]
    [BsonIgnoreIfNull]
    public long? CompletedRideCount { get; set; }
}
