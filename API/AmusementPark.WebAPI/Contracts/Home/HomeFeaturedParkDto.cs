using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.Home;

/// <summary>
/// Contrat HTTP d'un parc mis en avant sur la home publique.
/// </summary>
public sealed class HomeFeaturedParkDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? CountryCode { get; set; }

    public ParkTypeDto? Type { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public string? City { get; set; }

    public string? CurrentLogoImageId { get; set; }

    public bool IsManualFeatured { get; set; }

    public bool IsSponsoredFeatured { get; set; }

    public List<HomeFeaturedParkCategoryCountDto> CountsByCategory { get; set; } = new();
}
