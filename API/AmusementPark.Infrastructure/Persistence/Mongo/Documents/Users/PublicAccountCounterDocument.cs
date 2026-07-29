using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

public sealed class PublicAccountCounterDocument
{
    [BsonId]
    public string Id { get; set; } = "public-account-number";

    [BsonElement("sequence")]
    public long Sequence { get; set; }
}
