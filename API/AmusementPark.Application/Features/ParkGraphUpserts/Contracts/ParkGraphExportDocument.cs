using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportDocument
{
    public string DocumentType { get; init; } = "AmusementParkParkGraphUpsert";

    public string SchemaVersion { get; init; } = "2026-06-30";

    public string Mode { get; init; } = "merge";

    public ParkGraphExportIdentity Identity { get; init; } = new ParkGraphExportIdentity();

    public ParkGraphExportReferences References { get; init; } = new ParkGraphExportReferences();

    public ParkGraphExportPark Park { get; init; } = new ParkGraphExportPark();

    public List<ParkGraphExportZone> Zones { get; init; } = new List<ParkGraphExportZone>();

    public List<ParkGraphExportItem> Items { get; init; } = new List<ParkGraphExportItem>();

    public List<ParkGraphExportImage> Images { get; init; } = new List<ParkGraphExportImage>();

    public ParkGraphExportOpeningHours? OpeningHours { get; init; }

    public ParkGraphExportHistory History { get; init; } = new ParkGraphExportHistory();

    public ParkGraphExportMetadata Metadata { get; init; } = new ParkGraphExportMetadata();
}

public sealed class ParkGraphExportIdentity
{
    public string? ParkId { get; init; }

    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? CountryCode { get; init; }
}

public sealed class ParkGraphExportReferences
{
    public List<ParkGraphExportFounder> Founders { get; init; } = new List<ParkGraphExportFounder>();

    public List<ParkGraphExportOperator> Operators { get; init; } = new List<ParkGraphExportOperator>();

    public List<ParkGraphExportManufacturer> Manufacturers { get; init; } = new List<ParkGraphExportManufacturer>();
}

public sealed class ParkGraphExportPark
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? CountryCode { get; init; }

    public ParkType? Type { get; init; }

    public ParkAudienceClassification? AudienceClassification { get; init; }

    public ParkStatus Status { get; init; } = ParkStatus.Operating;

    public DateTime? OpeningDate { get; init; }

    public DateTime? ClosingDate { get; init; }

    public string? OpeningDateText { get; init; }

    public string? ClosingDateText { get; init; }

    public string? FounderId { get; init; }

    public string? FounderKey { get; init; }

    public string? OperatorId { get; init; }

    public string? OperatorKey { get; init; }

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();

    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public bool IsFeaturedOnHome { get; init; }

    public int? FeaturedHomeOrder { get; init; }

    public bool IsFeaturedOnHomeSponsored { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? Street { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public sealed class ParkGraphExportZone
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public List<LocalizedText> Names { get; init; } = new List<LocalizedText>();

    public string? Slug { get; init; }

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();

    public bool IsVisible { get; init; }

    public int SortOrder { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public sealed class ParkGraphExportItem
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public ParkItemCategory Category { get; init; }

    public ParkItemType Type { get; init; }

    public string? Subtype { get; init; }

    public string? ZoneId { get; init; }

    public string? ZoneKey { get; init; }

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();

    public ParkGraphExportAttractionDetails? AttractionDetails { get; init; }

    public AttractionLocations? AttractionLocations { get; init; }

    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public sealed class ParkGraphExportAttractionDetails
{
    public string? ManufacturerId { get; init; }

    public string? ManufacturerKey { get; init; }

    public string? Model { get; init; }

    public string? ExternalSource { get; init; }

    public string? ExternalId { get; init; }

    public string? SourceUrl { get; init; }

    public string? Status { get; init; }

    public string? MaterialType { get; init; }

    public string? SeatingType { get; init; }

    public string? LaunchType { get; init; }

    public string? RestraintType { get; init; }

    public bool? IsLaunched { get; init; }

    public DateTime? OpeningDate { get; init; }

    public DateTime? ClosingDate { get; init; }

    public string? OpeningDateText { get; init; }

    public string? ClosingDateText { get; init; }

    public int? DurationInSeconds { get; init; }

    public int? CapacityPerHour { get; init; }

    public double? HeightInFeet { get; init; }

    public double? HeightInMeters { get; init; }

    public double? LengthInFeet { get; init; }

    public double? LengthInMeters { get; init; }

    public double? SpeedInMph { get; init; }

    public double? SpeedInKmH { get; init; }

    public double? DropInFeet { get; init; }

    public double? DropInMeters { get; init; }

    public int? InversionCount { get; init; }

    public int? TrainCount { get; init; }

    public int? CarsPerTrain { get; init; }

    public int? RidersPerVehicle { get; init; }

    public bool? HasSingleRider { get; init; }

    public bool? HasFastPass { get; init; }

    public bool? IsAccessibleForReducedMobility { get; init; }

    public bool? IsIndoor { get; init; }

    public AttractionWaterExposureLevel? WaterExposureLevel { get; init; }

    public List<AttractionAccessCondition> AccessConditions { get; init; } = new List<AttractionAccessCondition>();
}

public sealed class ParkGraphExportImage
{
    public string ImageId { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public ImageOwnerType OwnerType { get; init; }

    public string? OwnerId { get; init; }

    public string? OwnerKey { get; init; }

    public ImageCategory Category { get; init; }

    public bool IsPublished { get; init; }

    public bool IsCurrent { get; init; }

    public bool SetAsCurrent { get; init; }

    public bool WithWatermark { get; init; }

    public bool IsWatermarked { get; init; }

    public string? SourceUrl { get; init; }

    public string? InternalUrl { get; init; }

    public string? Description { get; init; }

    public List<LocalizedText> AltTexts { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Captions { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Credits { get; init; } = new List<LocalizedText>();

    public List<string> TagIds { get; init; } = new List<string>();

    public GeoPoint? GeoLocation { get; init; }

    public string? OriginalFileName { get; init; }

    public string? ContentType { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public long SizeInBytes { get; init; }
}

public sealed class ParkGraphExportOpeningHours
{
    public string ParkId { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public string? Notes { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public List<ParkGraphExportOpeningHoursRule> RegularRules { get; init; } = new List<ParkGraphExportOpeningHoursRule>();

    public List<ParkGraphExportOpeningHoursDateOverride> DateOverrides { get; init; } = new List<ParkGraphExportOpeningHoursDateOverride>();
}

public sealed class ParkGraphExportOpeningHoursRule
{
    public string? Id { get; init; }

    public string StartDate { get; init; } = string.Empty;

    public string EndDate { get; init; } = string.Empty;

    public List<string> DaysOfWeek { get; init; } = new List<string>();

    public bool IsClosed { get; init; }

    public List<LocalizedText> Labels { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Reasons { get; init; } = new List<LocalizedText>();

    public int SortOrder { get; init; }

    public List<ParkGraphExportOpeningHoursTimeRange> TimeRanges { get; init; } = new List<ParkGraphExportOpeningHoursTimeRange>();
}

public sealed class ParkGraphExportOpeningHoursDateOverride
{
    public string LocalDate { get; init; } = string.Empty;

    public bool IsClosed { get; init; }

    public List<LocalizedText> Labels { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Reasons { get; init; } = new List<LocalizedText>();

    public List<ParkGraphExportOpeningHoursTimeRange> TimeRanges { get; init; } = new List<ParkGraphExportOpeningHoursTimeRange>();
}

public sealed class ParkGraphExportOpeningHoursTimeRange
{
    public string OpensAt { get; init; } = string.Empty;

    public string ClosesAt { get; init; } = string.Empty;

    public bool ClosesNextDay { get; init; }

    public string? LastAdmissionAt { get; init; }

    public bool LastAdmissionNextDay { get; init; }
}

public sealed class ParkGraphExportHistory
{
    public List<ParkGraphExportHistoryEvent> Events { get; init; } = new List<ParkGraphExportHistoryEvent>();
}

public sealed class ParkGraphExportHistoryEvent
{
    public string Key { get; init; } = string.Empty;

    public HistoryEntityType EntityType { get; init; }

    public string Owner { get; init; } = string.Empty;

    public string OwnerId { get; init; } = string.Empty;

    public string? ParkId { get; init; }

    public string? ParkItemId { get; init; }

    public string? ItemKey { get; init; }

    public string? ParkItemKey { get; init; }

    public string? ContextParkId { get; init; }

    public int Year { get; init; }

    public int? Month { get; init; }

    public int? Day { get; init; }

    public HistoryDatePrecision DatePrecision { get; init; }

    public string EventType { get; init; } = string.Empty;

    public bool IsMajor { get; init; }

    public bool IsVisible { get; init; }

    public string? Slug { get; init; }

    public List<LocalizedText> Titles { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Summaries { get; init; } = new List<LocalizedText>();

    public string? MainImageId { get; init; }

    public string? PreviousName { get; init; }

    public string? NewName { get; init; }

    public string? PreviousLogoImageId { get; init; }

    public string? NewLogoImageId { get; init; }

    public string? PreviousOperatorId { get; init; }

    public string? NewOperatorId { get; init; }

    public string? LocationLabel { get; init; }

    public List<string> RelatedParkIds { get; init; } = new List<string>();

    public List<string> RelatedParkItemIds { get; init; } = new List<string>();

    public List<ParkGraphExportHistorySource> Sources { get; init; } = new List<ParkGraphExportHistorySource>();

    public ParkGraphExportHistoryArticle? Article { get; init; }
}

public sealed class ParkGraphExportHistoryArticle
{
    public string? Slug { get; init; }

    public List<LocalizedText> Titles { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Subtitles { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Summaries { get; init; } = new List<LocalizedText>();

    public string? MainImageId { get; init; }

    public List<ParkGraphExportHistoryArticleBlock> Blocks { get; init; } = new List<ParkGraphExportHistoryArticleBlock>();

    public List<ParkGraphExportHistorySource> Sources { get; init; } = new List<ParkGraphExportHistorySource>();

    public bool IsPublished { get; init; }
}

public sealed class ParkGraphExportHistoryArticleBlock
{
    public string Id { get; init; } = string.Empty;

    public HistoryArticleBlockType Type { get; init; }

    public int SortOrder { get; init; }

    public int? HeadingLevel { get; init; }

    public List<LocalizedText> Texts { get; init; } = new List<LocalizedText>();

    public string? ImageId { get; init; }

    public List<string> ImageIds { get; init; } = new List<string>();

    public List<LocalizedText> Captions { get; init; } = new List<LocalizedText>();
}

public sealed class ParkGraphExportHistorySource
{
    public string? Label { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? AccessedAt { get; init; }
}

public sealed class ParkGraphExportFounder
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Occupation { get; init; }

    public string? BirthDate { get; init; }

    public string? DeathDate { get; init; }

    public string? BirthPlace { get; init; }

    public string? NationalityCountryCode { get; init; }

    public string? WebsiteUrl { get; init; }

    public List<LocalizedText> Biography { get; init; } = new List<LocalizedText>();
}

public sealed class ParkGraphExportOperator
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? LegalName { get; init; }

    public int? FoundedYear { get; init; }

    public int? ClosedYear { get; init; }

    public ParkReferenceContactDetails? ContactDetails { get; init; }

    public List<LocalizedText> Description { get; init; } = new List<LocalizedText>();

    public AdminReviewStatus AdminReviewStatus { get; init; }
}

public sealed class ParkGraphExportManufacturer
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? LegalName { get; init; }

    public int? FoundedYear { get; init; }

    public int? ClosedYear { get; init; }

    public ParkReferenceContactDetails? ContactDetails { get; init; }

    public List<LocalizedText> Biography { get; init; } = new List<LocalizedText>();

    public bool IsVisible { get; init; } = true;

    public AdminReviewStatus AdminReviewStatus { get; init; }
}

public sealed class ParkGraphExportMetadata
{
    public string Source { get; init; } = "admin-park-graph-export";

    public DateTime ExportedAtUtc { get; init; }
}
