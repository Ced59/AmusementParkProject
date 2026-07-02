using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkCreateDto
{
    public string? Name { get; set; }

    public string? CountryCode { get; set; }

    public ParkTypeDto? Type { get; set; }

    public ParkAudienceClassificationDto? AudienceClassification { get; set; }

    public ParkStatusDto Status { get; set; } = ParkStatusDto.Operating;

    public DateTime? OpeningDate { get; set; }

    public DateTime? ClosingDate { get; set; }

    public string? OpeningDateText { get; set; }

    public string? ClosingDateText { get; set; }

    public string? FounderId { get; set; }

    public string? OperatorId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;

    public bool IsFeaturedOnHome { get; set; }

    public int? FeaturedHomeOrder { get; set; }

    public bool IsFeaturedOnHomeSponsored { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }
}
