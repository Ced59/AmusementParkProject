using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Seo.Services;

internal sealed record HistorySitemapResolvedData(
    IReadOnlyCollection<string> Languages,
    IReadOnlyCollection<HistoryEvent> Events,
    IReadOnlyDictionary<string, Park> PublicParkById,
    IReadOnlyDictionary<string, ParkItem> PublicItemById,
    IReadOnlyDictionary<string, StandaloneAttraction> PublicStandaloneAttractionById);
