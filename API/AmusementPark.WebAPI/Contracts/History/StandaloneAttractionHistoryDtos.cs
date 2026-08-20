using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.StandaloneAttractions;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class StandaloneAttractionHistoryTimelineDto
{
    public string EntityType { get; set; } = "StandaloneAttraction";

    public StandaloneAttractionDto StandaloneAttraction { get; set; } = new StandaloneAttractionDto();

    public List<HistoryTimelineEventDto> Events { get; set; } = new();

    public PaginationDto? Pagination { get; set; }

    public List<HistoryTimelinePageRangeDto> PageRanges { get; set; } = new();
}
