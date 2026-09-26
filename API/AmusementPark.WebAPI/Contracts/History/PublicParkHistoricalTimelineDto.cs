using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicParkHistoricalTimelineDto
{
    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public IReadOnlyCollection<PublicHistoricalTimelineEntryDto> Events { get; set; } =
        Array.Empty<PublicHistoricalTimelineEntryDto>();

    public PaginationDto Pagination { get; set; } = new PaginationDto();
}
