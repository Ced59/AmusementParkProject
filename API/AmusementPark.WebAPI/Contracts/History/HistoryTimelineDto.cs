using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoryTimelineDto
{
    public string EntityType { get; set; } = string.Empty;

    public ParkDto? Park { get; set; }

    public ParkItemDto? ParkItem { get; set; }

    public bool HasParkItemTimelineEvents { get; set; }

    public List<ParkItemDto> IncludedParkItems { get; set; } = new();

    public List<HistoryTimelineEventDto> Events { get; set; } = new();

    public PaginationDto? Pagination { get; set; }

    public List<HistoryTimelinePageRangeDto> PageRanges { get; set; } = new();
}
