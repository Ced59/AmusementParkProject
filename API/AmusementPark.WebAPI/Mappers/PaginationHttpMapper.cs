using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de conversion HTTP pour la pagination.
/// </summary>
public static class PaginationHttpMapper
{
    public static PagedQuery ToApplication(this PaginationRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PagedQuery(request.Page, request.Size);
    }

    public static PaginationRequestDto Override(this PaginationRequestDto request, int? page = null, int? size = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PaginationRequestDto
        {
            Page = page ?? request.Page,
            Size = size ?? request.Size,
        };
    }

    public static PaginationDto ToHttp<TItem>(this PagedResult<TItem> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new PaginationDto
        {
            TotalItems = checked((int)result.TotalItems),
            TotalPages = result.TotalPages,
            CurrentPage = result.Page,
            ItemsPerPage = result.PageSize,
        };
    }

    public static PagedResponseDto<TTarget> ToPagedResponse<TSource, TTarget>(this PagedResult<TSource> result, Func<TSource, TTarget> mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return new PagedResponseDto<TTarget>
        {
            Data = result.Items.Select(mapper).ToList(),
            Pagination = result.ToHttp(),
        };
    }

    public static PagedResponseDto<TTarget> ToPagedResponse<TSource, TTarget>(this PaginationRequestDto request, IReadOnlyCollection<TSource> items, Func<TSource, TTarget> mapper)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(mapper);

        int totalItems = items.Count;
        int skip = (request.Page - 1) * request.Size;
        IReadOnlyCollection<TTarget> pagedItems = items.Skip(skip).Take(request.Size).Select(mapper).ToList();
        PagedResult<TTarget> result = new PagedResult<TTarget>(pagedItems, request.Page, request.Size, totalItems);
        return result.ToPagedResponse(static item => item);
    }
}
