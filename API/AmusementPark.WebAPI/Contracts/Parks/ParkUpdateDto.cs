using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? CountryCode { get; set; }

    public ParkTypeDto? Type { get; set; }

    public string? FounderId { get; set; }

    public string? OperatorId { get; set; }

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public bool IsVisible { get; set; }

    public bool IsFeaturedOnHome { get; set; }

    public int? FeaturedHomeOrder { get; set; }

    public bool IsFeaturedOnHomeSponsored { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }
}
