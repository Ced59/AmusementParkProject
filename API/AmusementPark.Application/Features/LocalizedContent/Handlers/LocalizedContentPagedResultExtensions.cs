using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Queries;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

internal static class LocalizedContentPagedResultExtensions
{
    public static PagedResult<TTarget> Map<TSource, TTarget>(this PagedResult<TSource> source, Func<TSource, TTarget> mapper)
    {
        return new PagedResult<TTarget>(source.Items.Select(mapper).ToList(), source.Page, source.PageSize, source.TotalItems);
    }
}
