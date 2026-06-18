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

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }
}

/// <summary>
/// Document embarqué des détails d'attraction.
/// </summary>
public sealed class AttractionDetailsDocument
{
    [BsonElement("manufacturerId")]
    [BsonIgnoreIfNull]
    public string? ManufacturerId { get; set; }

    [BsonElement("model")]
    [BsonIgnoreIfNull]
    public string? Model { get; set; }

    [BsonElement("externalSource")]
    [BsonIgnoreIfNull]
    public string? ExternalSource { get; set; }

    [BsonElement("externalId")]
    [BsonIgnoreIfNull]
    public string? ExternalId { get; set; }

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("status")]
    [BsonIgnoreIfNull]
    public string? Status { get; set; }

    [BsonElement("materialType")]
    [BsonIgnoreIfNull]
    public string? MaterialType { get; set; }

    [BsonElement("seatingType")]
    [BsonIgnoreIfNull]
    public string? SeatingType { get; set; }

    [BsonElement("launchType")]
    [BsonIgnoreIfNull]
    public string? LaunchType { get; set; }

    [BsonElement("restraintType")]
    [BsonIgnoreIfNull]
    public string? RestraintType { get; set; }

    [BsonElement("isLaunched")]
    [BsonIgnoreIfNull]
    public bool? IsLaunched { get; set; }

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

    [BsonElement("durationInSeconds")]
    [BsonIgnoreIfNull]
    public int? DurationInSeconds { get; set; }

    [BsonElement("capacityPerHour")]
    [BsonIgnoreIfNull]
    public int? CapacityPerHour { get; set; }

    [BsonElement("heightInFeet")]
    [BsonIgnoreIfNull]
    public double? HeightInFeet { get; set; }

    [BsonElement("heightInMeters")]
    [BsonIgnoreIfNull]
    public double? HeightInMeters { get; set; }

    [BsonElement("lengthInFeet")]
    [BsonIgnoreIfNull]
    public double? LengthInFeet { get; set; }

    [BsonElement("lengthInMeters")]
    [BsonIgnoreIfNull]
    public double? LengthInMeters { get; set; }

    [BsonElement("speedInMph")]
    [BsonIgnoreIfNull]
    public double? SpeedInMph { get; set; }

    [BsonElement("speedInKmH")]
    [BsonIgnoreIfNull]
    public double? SpeedInKmH { get; set; }

    [BsonElement("dropInFeet")]
    [BsonIgnoreIfNull]
    public double? DropInFeet { get; set; }

    [BsonElement("dropInMeters")]
    [BsonIgnoreIfNull]
    public double? DropInMeters { get; set; }

    [BsonElement("inversionCount")]
    [BsonIgnoreIfNull]
    public int? InversionCount { get; set; }

    [BsonElement("trainCount")]
    [BsonIgnoreIfNull]
    public int? TrainCount { get; set; }

    [BsonElement("carsPerTrain")]
    [BsonIgnoreIfNull]
    public int? CarsPerTrain { get; set; }

    [BsonElement("ridersPerVehicle")]
    [BsonIgnoreIfNull]
    public int? RidersPerVehicle { get; set; }

    [BsonElement("hasSingleRider")]
    [BsonIgnoreIfNull]
    public bool? HasSingleRider { get; set; }

    [BsonElement("hasFastPass")]
    [BsonIgnoreIfNull]
    public bool? HasFastPass { get; set; }

    [BsonElement("isAccessibleForReducedMobility")]
    [BsonIgnoreIfNull]
    public bool? IsAccessibleForReducedMobility { get; set; }

    [BsonElement("isIndoor")]
    [BsonIgnoreIfNull]
    public bool? IsIndoor { get; set; }

    [BsonElement("waterExposureLevel")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public AttractionWaterExposureLevel? WaterExposureLevel { get; set; }

    [BsonElement("accessConditions")]
    public List<AttractionAccessConditionDocument> AccessConditions { get; set; } = new();
}

/// <summary>
/// Document embarqué d'une contrainte d'accès.
/// </summary>
public sealed class AttractionAccessConditionDocument
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionType Type { get; set; }

    [BsonElement("typeKey")]
    [BsonIgnoreIfNull]
    public string? TypeKey { get; set; }

    [BsonElement("isCustom")]
    [BsonIgnoreIfNull]
    public bool? IsCustom { get; set; }

    [BsonElement("customTypeKey")]
    [BsonIgnoreIfNull]
    public string? CustomTypeKey { get; set; }

    [BsonElement("customTypeLabel")]
    public List<LocalizedTextDocument> CustomTypeLabel { get; set; } = new();

    [BsonElement("value")]
    [BsonIgnoreIfNull]
    public double? Value { get; set; }

    [BsonElement("unit")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionUnit? Unit { get; set; }

    [BsonElement("requiresAccompaniment")]
    [BsonIgnoreIfNull]
    public bool? RequiresAccompaniment { get; set; }

    [BsonElement("minimumCompanionAge")]
    [BsonIgnoreIfNull]
    public int? MinimumCompanionAge { get; set; }

    [BsonElement("label")]
    public List<LocalizedTextDocument> Label { get; set; } = new();

    [BsonElement("description")]
    public List<LocalizedTextDocument> Description { get; set; } = new();

    [BsonElement("displayOrder")]
    [BsonIgnoreIfNull]
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Document embarqué des points fonctionnels d'une attraction.
/// </summary>
public sealed class AttractionLocationsDocument
{
    [BsonElement("entrance")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? Entrance { get; set; }

    [BsonElement("exit")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? Exit { get; set; }

    [BsonElement("fastPassEntrance")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? FastPassEntrance { get; set; }

    [BsonElement("reducedMobilityEntrance")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? ReducedMobilityEntrance { get; set; }
}

/// <summary>
/// Document Mongo d'un type réutilisable de condition d'accès.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class AttractionAccessConditionTypeDefinitionDocument : MongoDocumentBase
{
    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("legacyType")]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionType LegacyType { get; set; } = AttractionAccessConditionType.Custom;

    [BsonElement("isSystem")]
    public bool IsSystem { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }
}
