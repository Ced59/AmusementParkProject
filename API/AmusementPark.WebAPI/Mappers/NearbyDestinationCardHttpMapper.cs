using AmusementPark.Application.Features.NearbyDestinations.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.NearbyDestinations;

namespace AmusementPark.WebAPI.Mappers;

public static class NearbyDestinationCardHttpMapper
{
    public static NearbyDestinationCardDto ToCardHttp(this NearbyDestinationResult target)
    {
        Park? park = target.Park;
        StandaloneAttraction? attraction = target.StandaloneAttraction;
        return new NearbyDestinationCardDto(
            park is not null ? "Park" : "StandaloneAttraction",
            park?.Id ?? attraction!.Id, park is not null ? park.Name : attraction!.Name,
            park?.CountryCode ?? attraction?.CountryCode, park?.City ?? attraction?.City,
            park?.Street ?? attraction?.Street, park?.PostalCode ?? attraction?.PostalCode,
            park?.Position?.Latitude ?? attraction?.Position?.Latitude,
            park?.Position?.Longitude ?? attraction?.Position?.Longitude,
            park?.WebsiteUrl ?? attraction?.WebsiteUrl, park?.CurrentLogoImageId,
            target.PhotoImageId, park?.Status.ToString(), attraction?.AttractionDetails?.Status,
            (park?.Descriptions ?? attraction!.Descriptions).Select(static text => new LocalizedTextDto
            {
                LanguageCode = text.LanguageCode,
                Value = PublicCardTextHttpMapper.ToCardDescription(text.Value),
            }).ToArray(), target.DistanceKilometers, target.EstimatedTravelDurationMinutes, target.ProximityRank);
    }
}
