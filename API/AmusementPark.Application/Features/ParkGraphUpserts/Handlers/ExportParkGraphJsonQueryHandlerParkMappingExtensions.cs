using System.Globalization;
using System.Text;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Application.Common.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
internal static class ExportParkGraphJsonQueryHandlerParkMappingExtensions
{
    internal static ParkGraphExportPark MapPark(Park park)
    {
        return new ParkGraphExportPark
        {
            Id = park.Id,
            Name = park.Name,
            CountryCode = park.CountryCode,
            Type = park.Type,
            AudienceClassification = park.AudienceClassification,
            Status = park.Status,
            OpeningDate = park.OpeningDate,
            ClosingDate = park.ClosingDate,
            OpeningDateText = park.OpeningDateText,
            ClosingDateText = park.ClosingDateText,
            FounderId = park.FounderId,
            FounderKey = park.FounderId,
            OperatorId = park.OperatorId,
            OperatorKey = park.OperatorId,
            Descriptions = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(park.Descriptions),
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
