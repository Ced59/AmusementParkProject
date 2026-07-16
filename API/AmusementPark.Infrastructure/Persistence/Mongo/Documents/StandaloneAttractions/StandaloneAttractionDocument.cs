using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;

[BsonIgnoreExtraElements]
public sealed class StandaloneAttractionDocument : MongoGeolocatedDocumentBase
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ParkItemType Type { get; set; } = ParkItemType.Attraction;

    [BsonElement("subtype")]
    [BsonIgnoreIfNull]
    public string? Subtype { get; set; }

    [BsonElement("operatorId")]
    [BsonIgnoreIfNull]
    public string? OperatorId { get; set; }

    [BsonElement("websiteUrl")]
    [BsonIgnoreIfNull]
    public string? WebsiteUrl { get; set; }

    [BsonElement("street")]
    [BsonIgnoreIfNull]
    public string? Street { get; set; }

    [BsonElement("city")]
    [BsonIgnoreIfNull]
    public string? City { get; set; }

    [BsonElement("postalCode")]
    [BsonIgnoreIfNull]
    public string? PostalCode { get; set; }

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("attractionDetails")]
    [BsonIgnoreIfNull]
    public AttractionDetailsDocument? AttractionDetails { get; set; }

    [BsonElement("attractionLocations")]
    [BsonIgnoreIfNull]
    public AttractionLocationsDocument? AttractionLocations { get; set; }

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; }

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }

    [BsonElement("legacyParkId")]
    [BsonIgnoreIfNull]
    public string? LegacyParkId { get; set; }

    [BsonElement("legacyParkItemId")]
    [BsonIgnoreIfNull]
    public string? LegacyParkItemId { get; set; }
}

