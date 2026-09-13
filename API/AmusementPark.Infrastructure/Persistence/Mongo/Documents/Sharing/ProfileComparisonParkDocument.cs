using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

public sealed class ProfileComparisonParkDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("creatorVisitCount")]
    [BsonIgnoreIfNull]
    public long? CreatorVisitCount { get; set; }

    [BsonElement("acceptorVisitCount")]
    [BsonIgnoreIfNull]
    public long? AcceptorVisitCount { get; set; }
}
