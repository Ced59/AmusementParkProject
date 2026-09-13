using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorSocialPublishingExtensions
{
    internal static async Task PublishNewlyVisibleParkAsync(this ParkGraphUpsertProcessor processorContext, Park park, bool wasPubliclyDiscoverable, string? requestedByUserId, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        if (processorContext.socialPublicationService is null || wasPubliclyDiscoverable || !park.IsPubliclyDiscoverable())
        {
            return;
        }

        try
        {
            SocialPublication? publication = await processorContext.socialPublicationService.PublishParkAnnouncementAsync(park, requestedByUserId, cancellationToken);
            if (publication?.Status == SocialPublicationStatus.Failed)
            {
                result.Warnings.Add("Le parc est maintenant public, mais son annonce Facebook n'a pas pu être envoyée. Elle peut être relancée depuis l'administration des publications sociales.");
            }
        }
        catch (OperationCanceledException)when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result.Warnings.Add("Le parc est maintenant public, mais la préparation de son annonce Facebook a échoué. Vérifier l'administration des publications sociales.");
        }
    }
}
