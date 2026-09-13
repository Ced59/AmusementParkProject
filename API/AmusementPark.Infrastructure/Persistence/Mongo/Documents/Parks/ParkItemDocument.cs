using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document Mongo d'un élément de parc.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class ParkItemDocument : MongoGeolocatedDocumentBase
{
    [BsonElement("parkId")]
    [BsonRepresentation(BsonType.String)]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("zoneId")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public string? ZoneId { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("category")]
    [BsonRepresentation(BsonType.String)]
    public ParkItemCategory Category { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ParkItemType Type { get; set; }

    [BsonElement("subtype")]
    [BsonIgnoreIfNull]
    public string? Subtype { get; set; }

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("attractionDetails")]
    [BsonIgnoreIfNull]
    public AttractionDetailsDocument? AttractionDetails { get; set; }

    [BsonElement("attractionLocations")]
    [BsonIgnoreIfNull]
    public AttractionLocationsDocument? AttractionLocations { get; set; }

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; } = true;

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }
}
