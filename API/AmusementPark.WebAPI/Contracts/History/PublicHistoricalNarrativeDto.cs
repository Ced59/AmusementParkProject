using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalNarrativeDto
{
    public string EventId { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Titles { get; set; } =
        Array.Empty<LocalizedTextDto>();
}
