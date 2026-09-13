using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoryEventDto
{
    public string? Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;

    public string? ParkId { get; set; }

    public string? ParkItemId { get; set; }

    public string? ContextParkId { get; set; }

    public int Year { get; set; }

    public int? Month { get; set; }

    public int? Day { get; set; }

    public string DatePrecision { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public bool IsMajor { get; set; }

    public bool IsVisible { get; set; } = true;

    public string? Slug { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }

    public string? PreviousName { get; set; }

    public string? NewName { get; set; }

    public string? PreviousLogoImageId { get; set; }

    public string? NewLogoImageId { get; set; }

    public string? PreviousOperatorId { get; set; }

    public string? NewOperatorId { get; set; }

    public string? LocationLabel { get; set; }

    public List<string> RelatedParkIds { get; set; } = new();

    public List<string> RelatedParkItemIds { get; set; } = new();

    public List<HistorySourceReferenceDto> Sources { get; set; } = new();

    public HistoryArticleContentDto? Article { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
