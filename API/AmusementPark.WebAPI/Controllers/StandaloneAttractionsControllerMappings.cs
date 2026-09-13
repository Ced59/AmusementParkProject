using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Application.Features.Countries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.StandaloneAttractions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

internal static class StandaloneAttractionsControllerMappings
{
    public static ParkItemType ToDomainForStandaloneController(this ParkItemTypeDto value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemType parsed) ? parsed : ParkItemType.Attraction;
    }
}
