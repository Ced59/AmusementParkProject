using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Home;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Mappers HTTP dédiés à la home publique.
/// </summary>
internal static class HomeHttpMappers
{
    public static HomeFeaturedParkDto ToHttp(this HomeFeaturedParkResult value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(value.Park);

        return new HomeFeaturedParkDto
        {
            Id = value.Park.Id,
            Name = value.Park.Name,
            CountryCode = value.Park.CountryCode,
            Type = value.Park.Type.ToHomeHttp(),
            Latitude = value.Park.Position?.Latitude ?? 0.0,
            Longitude = value.Park.Position?.Longitude ?? 0.0,
            Descriptions = value.Park.Descriptions.ToHttp(),
            City = value.Park.City,
            CurrentLogoImageId = value.Park.CurrentLogoImageId,
            IsManualFeatured = value.IsManualFeatured,
            IsSponsoredFeatured = value.IsManualFeatured && value.Park.IsFeaturedOnHomeSponsored,
            CountsByCategory = value.CountsByCategory
                .Where(static pair => pair.Value > 0)
                .OrderBy(static pair => pair.Key == ParkItemCategory.Attraction ? 0 : 1)
                .ThenBy(static pair => pair.Key.ToString(), StringComparer.Ordinal)
                .Select(static pair => new HomeFeaturedParkCategoryCountDto
                {
                    Category = pair.Key.ToString(),
                    Count = pair.Value,
                })
                .ToList(),
        };
    }

    private static ParkTypeDto? ToHomeHttp(this ParkType? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Enum.TryParse(value.Value.ToString(), out ParkTypeDto parsed) ? parsed : null;
    }
}
