using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Attraction fixe publiable sans parc parent.
/// </summary>
public sealed class StandaloneAttraction : GeolocatedEntityBase
{
    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public ParkItemType Type { get; set; } = ParkItemType.Attraction;

    public string? Subtype { get; set; }

    public string? OperatorId { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public List<LocalizedText> Descriptions { get; set; } = new();

    public AttractionDetails? AttractionDetails { get; set; }

    public AttractionLocations? AttractionLocations { get; set; }

    public bool IsVisible { get; set; }

    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    public string? LegacyParkId { get; set; }

    public string? LegacyParkItemId { get; set; }

    public bool IsPubliclyPublishable()
    {
        return !string.IsNullOrWhiteSpace(this.Id)
            && !string.IsNullOrWhiteSpace(this.Name)
            && this.IsVisible
            && this.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && !ParkItemStatusNormalizer.IsClosedDefinitively(this.AttractionDetails?.Status);
    }
}

