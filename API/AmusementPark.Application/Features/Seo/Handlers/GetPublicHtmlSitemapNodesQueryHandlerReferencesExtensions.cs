using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Core.Domain.TechnicalPages;
using System.Xml;
using System.Xml.Linq;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.Videos.Ports;

namespace AmusementPark.Application.Features.Seo.Handlers;
internal static class GetPublicHtmlSitemapNodesQueryHandlerReferencesExtensions
{
    internal static IReadOnlyCollection<PublicHtmlSitemapNode> BuildReferenceGroupNodes(string language)
    {
        return new List<PublicHtmlSitemapNode>
        {
            new PublicHtmlSitemapNode
            {
                Id = "reference-manufacturers",
                Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "manufacturers"),
                RelativeUrl = $"/{language}/manufacturers",
                HasChildren = true
            },
            new PublicHtmlSitemapNode
            {
                Id = "reference-operators",
                Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "operators"),
                HasChildren = true
            },
            new PublicHtmlSitemapNode
            {
                Id = "reference-founders",
                Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "founders"),
                HasChildren = true
            },
        };
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildOperatorNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkOperator> operators = await processorContext.parkOperatorRepository.GetAllAsync(cancellationToken);
        return operators.Where(static entity => !string.IsNullOrWhiteSpace(entity.Id) && !string.IsNullOrWhiteSpace(entity.Name) && entity.AdminReviewStatus != AdminReviewStatus.NotRelevant).OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase).Select(entity => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"operator:{entity.Id}", entity.Name, $"/{language}/park-operator/{entity.Id}/{SeoSlugService.ToSlug(entity.Name, "reference")}")).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildFounderNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkFounder> founders = await processorContext.parkFounderRepository.GetAllAsync(cancellationToken);
        return founders.Where(static entity => !string.IsNullOrWhiteSpace(entity.Id) && !string.IsNullOrWhiteSpace(entity.Name)).OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase).Select(entity => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"founder:{entity.Id}", entity.Name, $"/{language}/park-founder/{entity.Id}/{SeoSlugService.ToSlug(entity.Name, "reference")}")).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildManufacturerNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AttractionManufacturer> manufacturers = await processorContext.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        return manufacturers.Where(static entity => !string.IsNullOrWhiteSpace(entity.Id) && !string.IsNullOrWhiteSpace(entity.Name) && entity.IsVisible && entity.AdminReviewStatus != AdminReviewStatus.NotRelevant).OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase).Select(entity => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"manufacturer:{entity.Id}", entity.Name, $"/{language}/park-manufacturer/{entity.Id}/{SeoSlugService.ToSlug(entity.Name, "reference")}")).ToList();
    }
}
