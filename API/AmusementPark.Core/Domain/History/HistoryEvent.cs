using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.History;

public sealed class HistoryEvent : AuditableEntity
{
    public string Key { get; set; } = string.Empty;

    public HistoryEntityType EntityType { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public string? ParkId { get; set; }

    public string? ParkItemId { get; set; }

    public string? ContextParkId { get; set; }

    public int Year { get; set; }

    public int? Month { get; set; }

    public int? Day { get; set; }

    public HistoryDatePrecision DatePrecision { get; set; } = HistoryDatePrecision.Year;

    public string EventType { get; set; } = string.Empty;

    public bool IsMajor { get; set; }

    public bool IsVisible { get; set; } = true;

    public string? Slug { get; set; }

    public List<LocalizedText> Titles { get; set; } = new();

    public List<LocalizedText> Summaries { get; set; } = new();

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

    public List<HistorySourceReference> Sources { get; set; } = new();

    public HistoryArticle? Article { get; set; }
}
