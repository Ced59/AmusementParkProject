using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;

[BsonIgnoreExtraElements]
public sealed class VideoTagDocument : MongoDocumentBase
{
    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
