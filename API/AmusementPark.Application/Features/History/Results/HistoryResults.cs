using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed class HistoryTimelineResult
{
    public HistoryEntityType EntityType { get; init; }

    public Park? Park { get; init; }

    public ParkItem? ParkItem { get; init; }

    public bool HasParkItemTimelineEvents { get; init; }

    public IReadOnlyCollection<ParkItem> IncludedParkItems { get; init; } = Array.Empty<ParkItem>();

    public IReadOnlyCollection<HistoryTimelineEventResult> Events { get; init; } = Array.Empty<HistoryTimelineEventResult>();

    public HistoryTimelinePaginationResult Pagination { get; init; } = new HistoryTimelinePaginationResult();

    public IReadOnlyCollection<HistoryTimelinePageRangeResult> PageRanges { get; init; } = Array.Empty<HistoryTimelinePageRangeResult>();
}

public sealed class HistoryTimelinePaginationResult
{
    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public int CurrentPage { get; init; } = 1;

    public int ItemsPerPage { get; init; } = HistoryTimelinePaging.DefaultPageSize;
}

public sealed class HistoryTimelinePageRangeResult
{
    public int Page { get; init; }

    public int StartYear { get; init; }

    public int EndYear { get; init; }

    public int EventCount { get; init; }
}

public sealed class HistoryTimelineEventResult
{
    public HistoryEvent Event { get; init; } = new HistoryEvent();

    public Park? ContextPark { get; init; }

    public ParkItem? ParkItem { get; init; }

    public Image? MainImage { get; init; }
}

public sealed class HistoryArticleResult
{
    public HistoryEvent Event { get; init; } = new HistoryEvent();

    public Park? Park { get; init; }

    public ParkItem? ParkItem { get; init; }

    public Park? ContextPark { get; init; }

    public Image? MainImage { get; init; }
}
