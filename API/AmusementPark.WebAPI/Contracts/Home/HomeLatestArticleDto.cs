using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Home;

public sealed class HomeLatestArticleDto
{
    public string? EventId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string? ParkId { get; set; }

    public string? ParkName { get; set; }

    public string? ParkItemId { get; set; }

    public string? ParkItemName { get; set; }

    public string? Slug { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }
}
