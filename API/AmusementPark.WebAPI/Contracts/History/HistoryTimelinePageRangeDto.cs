using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoryTimelinePageRangeDto
{
    public int Page { get; set; }

    public int StartYear { get; set; }

    public int EndYear { get; set; }

    public int EventCount { get; set; }
}
