using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;

namespace AmusementPark.Application.Features.Seo.Services;

internal sealed record PublicSeoParkItemsByParkId(
    IReadOnlyDictionary<string, List<PublicSeoParkItemSnapshot>> PublicItemsByParkId,
    IReadOnlyDictionary<string, List<PublicSeoParkItemSnapshot>> HistoryItemsByParkId);
