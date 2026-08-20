using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Results;

public sealed class StandaloneAttractionHistoryTimelineResult
{
    public HistoryEntityType EntityType { get; init; } = HistoryEntityType.StandaloneAttraction;

    public StandaloneAttraction StandaloneAttraction { get; init; } = new StandaloneAttraction();

    public IReadOnlyCollection<HistoryTimelineEventResult> Events { get; init; } = Array.Empty<HistoryTimelineEventResult>();

    public HistoryTimelinePaginationResult? Pagination { get; init; }

    public IReadOnlyCollection<HistoryTimelinePageRangeResult> PageRanges { get; init; } = Array.Empty<HistoryTimelinePageRangeResult>();
}
