using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Coordonnées facultatives d'une référence liée aux parcs.
/// </summary>
public sealed class ParkReferenceContactDetailsDocument
{
    [BsonElement("websiteUrl")]
    [BsonIgnoreIfNull]
    public string? WebsiteUrl { get; set; }

    [BsonElement("email")]
    [BsonIgnoreIfNull]
    public string? Email { get; set; }

    [BsonElement("phoneNumber")]
    [BsonIgnoreIfNull]
    public string? PhoneNumber { get; set; }

    [BsonElement("street")]
    [BsonIgnoreIfNull]
    public string? Street { get; set; }

    [BsonElement("city")]
    [BsonIgnoreIfNull]
    public string? City { get; set; }

    [BsonElement("postalCode")]
    [BsonIgnoreIfNull]
    public string? PostalCode { get; set; }

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("latitude")]
    [BsonIgnoreIfNull]
    public double? Latitude { get; set; }

    [BsonElement("longitude")]
    [BsonIgnoreIfNull]
    public double? Longitude { get; set; }
}

/// <summary>
/// Document Mongo d'un fondateur de parc.
/// </summary>
public sealed class ParkFounderDocument : MongoDocumentBase
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("occupation")]
    [BsonIgnoreIfNull]
    public string? Occupation { get; set; }

    [BsonElement("birthDate")]
    [BsonIgnoreIfNull]
    public string? BirthDate { get; set; }

    [BsonElement("deathDate")]
    [BsonIgnoreIfNull]
    public string? DeathDate { get; set; }

    [BsonElement("birthPlace")]
    [BsonIgnoreIfNull]
    public string? BirthPlace { get; set; }

    [BsonElement("nationalityCountryCode")]
    [BsonIgnoreIfNull]
    public string? NationalityCountryCode { get; set; }

    [BsonElement("websiteUrl")]
    [BsonIgnoreIfNull]
    public string? WebsiteUrl { get; set; }

    [BsonElement("biography")]
    public List<LocalizedTextDocument> Biography { get; set; } = new();
}

/// <summary>
/// Document Mongo d'un exploitant de parc.
/// </summary>
public sealed class ParkOperatorDocument : MongoDocumentBase
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("legalName")]
    [BsonIgnoreIfNull]
    public string? LegalName { get; set; }

    [BsonElement("foundedYear")]
    [BsonIgnoreIfNull]
    public int? FoundedYear { get; set; }

    [BsonElement("closedYear")]
    [BsonIgnoreIfNull]
    public int? ClosedYear { get; set; }

    [BsonElement("contactDetails")]
    [BsonIgnoreIfNull]
    public ParkReferenceContactDetailsDocument? ContactDetails { get; set; }

    [BsonElement("description")]
    public List<LocalizedTextDocument> Description { get; set; } = new();

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }
}

/// <summary>
/// Document Mongo d'un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerDocument : MongoDocumentBase
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("legalName")]
    [BsonIgnoreIfNull]
    public string? LegalName { get; set; }

    [BsonElement("foundedYear")]
    [BsonIgnoreIfNull]
    public int? FoundedYear { get; set; }

    [BsonElement("closedYear")]
    [BsonIgnoreIfNull]
    public int? ClosedYear { get; set; }

    [BsonElement("contactDetails")]
    [BsonIgnoreIfNull]
    public ParkReferenceContactDetailsDocument? ContactDetails { get; set; }

    [BsonElement("biography")]
    public List<LocalizedTextDocument> Biography { get; set; } = new();

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }
}

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
}

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
}
