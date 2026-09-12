using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed class HistoryTimelinePaginationResult
{
    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public int CurrentPage { get; init; } = 1;

    public int ItemsPerPage { get; init; } = HistoryTimelinePaging.DefaultPageSize;
}
