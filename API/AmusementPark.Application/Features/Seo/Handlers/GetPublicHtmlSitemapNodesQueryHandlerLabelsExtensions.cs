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
internal static class GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions
{
    internal static string Label(string language, string key)
    {
        if (GetPublicHtmlSitemapNodesQueryHandler.SiteLabels.TryGetValue(language, out IReadOnlyDictionary<string, string>? labels) && labels.TryGetValue(key, out string? label))
        {
            return label;
        }

        return GetPublicHtmlSitemapNodesQueryHandler.SiteLabels["en"].TryGetValue(key, out string? fallback) ? fallback : key;
    }
}
