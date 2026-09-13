using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document Mongo d'une zone de parc.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class ParkZoneDocument : MongoGeolocatedDocumentBase
{
    [BsonElement("parkId")]
    [BsonRepresentation(BsonType.String)]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("names")]
    public List<LocalizedTextDocument> Names { get; set; } = new();

    [BsonElement("slug")]
    [BsonIgnoreIfNull]
    public string? Slug { get; set; }

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; } = true;

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }
}
