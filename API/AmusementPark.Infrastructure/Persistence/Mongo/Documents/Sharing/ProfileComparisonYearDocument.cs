using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

public sealed class ProfileComparisonYearDocument
{
    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("creatorVisitCount")]
    public long CreatorVisitCount { get; set; }

    [BsonElement("acceptorVisitCount")]
    public long AcceptorVisitCount { get; set; }

    [BsonElement("creatorRideCount")]
    [BsonIgnoreIfNull]
    public long? CreatorRideCount { get; set; }

    [BsonElement("acceptorRideCount")]
    [BsonIgnoreIfNull]
    public long? AcceptorRideCount { get; set; }
}
