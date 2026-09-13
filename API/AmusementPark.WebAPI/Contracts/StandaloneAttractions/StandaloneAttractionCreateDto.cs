using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.StandaloneAttractions;

public class StandaloneAttractionCreateDto
{
    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public ParkItemTypeDto Type { get; set; } = ParkItemTypeDto.Attraction;

    public string? Subtype { get; set; }

    public string? OperatorId { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public AttractionDetailsDto? AttractionDetails { get; set; }

    public AttractionLocationsDto? AttractionLocations { get; set; }

    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.ToReview;

    public string? LegacyParkId { get; set; }

    public string? LegacyParkItemId { get; set; }
}
