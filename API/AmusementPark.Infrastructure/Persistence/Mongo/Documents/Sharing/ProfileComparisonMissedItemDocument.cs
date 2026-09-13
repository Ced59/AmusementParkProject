using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

public sealed class ProfileComparisonMissedItemDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("creatorOccurrenceCount")]
    [BsonIgnoreIfNull]
    public long? CreatorOccurrenceCount { get; set; }

    [BsonElement("acceptorOccurrenceCount")]
    [BsonIgnoreIfNull]
    public long? AcceptorOccurrenceCount { get; set; }
}
