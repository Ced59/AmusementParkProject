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
