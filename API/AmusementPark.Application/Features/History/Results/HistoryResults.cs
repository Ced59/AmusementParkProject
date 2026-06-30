using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Results;

public sealed class HistoryTimelineResult
{
    public HistoryEntityType EntityType { get; init; }

    public Park? Park { get; init; }

    public ParkItem? ParkItem { get; init; }

    public IReadOnlyCollection<ParkItem> IncludedParkItems { get; init; } = Array.Empty<ParkItem>();

    public IReadOnlyCollection<HistoryTimelineEventResult> Events { get; init; } = Array.Empty<HistoryTimelineEventResult>();
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
