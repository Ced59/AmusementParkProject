using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document Mongo d'un parc.
/// </summary>
public sealed class ParkDocument : MongoGeolocatedDocumentBase
{
    [BsonElement("name")]
    [BsonIgnoreIfNull]
    public string? Name { get; set; }

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("type")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public ParkType? Type { get; set; }

    [BsonElement("audienceClassification")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public ParkAudienceClassification? AudienceClassification { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ParkStatus Status { get; set; } = ParkStatus.Operating;

    [BsonElement("openingDate")]
    [BsonIgnoreIfNull]
    public DateTime? OpeningDate { get; set; }

    [BsonElement("closingDate")]
    [BsonIgnoreIfNull]
    public DateTime? ClosingDate { get; set; }

    [BsonElement("openingDateText")]
    [BsonIgnoreIfNull]
    public string? OpeningDateText { get; set; }

    [BsonElement("closingDateText")]
    [BsonIgnoreIfNull]
    public string? ClosingDateText { get; set; }

    [BsonElement("founderId")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public string? FounderId { get; set; }

    [BsonElement("operatorId")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public string? OperatorId { get; set; }

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; }

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }

    [BsonElement("isFeaturedOnHome")]
    public bool IsFeaturedOnHome { get; set; }

    [BsonElement("featuredHomeOrder")]
    [BsonIgnoreIfNull]
    public int? FeaturedHomeOrder { get; set; }

    [BsonElement("isFeaturedOnHomeSponsored")]
    public bool IsFeaturedOnHomeSponsored { get; set; }

    [BsonElement("randomSortKey")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.Double)]
    public double? RandomSortKey { get; set; }

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

    [BsonElement("currentLogoImageId")]
    [BsonIgnoreIfNull]
    public string? CurrentLogoImageId { get; set; }

    [BsonElement("officialMaps")]
    public List<ParkOfficialMapDocument> OfficialMaps { get; set; } = new List<ParkOfficialMapDocument>();
}
