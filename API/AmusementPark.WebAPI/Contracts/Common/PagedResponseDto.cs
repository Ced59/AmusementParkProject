using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Enveloppe HTTP paginée alignée sur le legacy.
/// </summary>
/// <typeparam name="TItem">Type des éléments retournés.</typeparam>
public sealed class PagedResponseDto<TItem>
{
    public IReadOnlyCollection<TItem> Data { get; set; } = Array.Empty<TItem>();

    public PaginationDto? Pagination { get; set; }
}
