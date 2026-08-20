using AmusementPark.Application.Features.History.Results;
using AmusementPark.WebAPI.Contracts.History;

namespace AmusementPark.WebAPI.Mappers;

internal static class StandaloneAttractionHistoryHttpMappers
{
    public static StandaloneAttractionHistoryTimelineDto ToHttp(this StandaloneAttractionHistoryTimelineResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new StandaloneAttractionHistoryTimelineDto
        {
            EntityType = value.EntityType.ToString(),
            StandaloneAttraction = value.StandaloneAttraction.ToHttp(),
            Events = value.Events.Select(static item => item.ToHttp()).ToList(),
            Pagination = value.Pagination?.ToHttp(),
            PageRanges = value.PageRanges.Select(static range => range.ToHttp()).ToList(),
        };
    }
}
