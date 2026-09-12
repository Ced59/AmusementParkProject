using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed class HistoryArticleResult
{
    public HistoryEvent Event { get; init; } = new HistoryEvent();

    public Park? Park { get; init; }

    public ParkItem? ParkItem { get; init; }

    public Park? ContextPark { get; init; }

    public Image? MainImage { get; init; }
}
