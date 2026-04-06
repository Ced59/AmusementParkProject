using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;

/// <summary>
/// Projection Mongo dédiée à la recherche.
/// </summary>
public sealed class SearchItemDocument : MongoGeolocatedDocumentBase
{
    [BsonElement("originalId")]
    public string OriginalId { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("resourceType")]
    [BsonIgnoreIfNull]
    public string? ResourceType { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("subtitle")]
    [BsonIgnoreIfNull]
    public string? Subtitle { get; set; }

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("keywords")]
    public List<string> Keywords { get; set; } = new();

    [BsonElement("compositeScore")]
    [BsonRepresentation(BsonType.Double)]
    public double CompositeScore { get; set; }

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; }
}
