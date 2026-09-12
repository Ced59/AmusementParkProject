using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed class HistoryTimelinePageRangeResult
{
    public int Page { get; init; }

    public int StartYear { get; init; }

    public int EndYear { get; init; }

    public int EventCount { get; init; }
}
