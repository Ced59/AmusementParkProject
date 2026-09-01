using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

[BsonIgnoreExtraElements]
public sealed class RatingRankingSourceRevisionDocument : MongoDocumentBase
{
    [BsonElement("scopeKey")]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("revision")]
    public long Revision { get; set; }
}
