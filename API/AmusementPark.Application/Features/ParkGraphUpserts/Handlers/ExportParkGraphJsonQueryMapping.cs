using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed partial class ExportParkGraphJsonQueryHandler
{
    private static ParkGraphExportPark MapPark(Park park)
    {
        return new ParkGraphExportPark
        {
            Id = park.Id,
            Name = park.Name,
            CountryCode = park.CountryCode,
            Type = park.Type,
            Status = park.Status,
            OpeningDate = park.OpeningDate,
            ClosingDate = park.ClosingDate,
            OpeningDateText = park.OpeningDateText,
            ClosingDateText = park.ClosingDateText,
            FounderId = park.FounderId,
            FounderKey = park.FounderId,
            OperatorId = park.OperatorId,
            OperatorKey = park.OperatorId,
            Descriptions = CopyLocalizedTexts(park.Descriptions),
            IsVisible = park.IsVisible,
            AdminReviewStatus = park.AdminReviewStatus,
            IsFeaturedOnHome = park.IsFeaturedOnHome,
            FeaturedHomeOrder = park.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = park.IsFeaturedOnHomeSponsored,
            WebsiteUrl = park.WebsiteUrl,
            Street = park.Street,
            City = park.City,
            PostalCode = park.PostalCode,
            Latitude = park.Position?.Latitude,
            Longitude = park.Position?.Longitude,
        };
    }
}
