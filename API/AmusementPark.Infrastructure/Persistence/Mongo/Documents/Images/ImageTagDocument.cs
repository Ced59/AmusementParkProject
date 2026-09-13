using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;

/// <summary>
/// Document Mongo d'un tag d'image.
/// </summary>
public sealed class ImageTagDocument : MongoDocumentBase
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
