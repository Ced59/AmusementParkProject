using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportHistoryEvent
{
    public string Key { get; init; } = string.Empty;

    public HistoryEntityType EntityType { get; init; }

    public string Owner { get; init; } = string.Empty;

    public string OwnerId { get; init; } = string.Empty;

    public string? ParkId { get; init; }

    public string? ParkItemId { get; init; }

    public string? ItemKey { get; init; }

    public string? ParkItemKey { get; init; }

    public string? ContextParkId { get; init; }

    public int Year { get; init; }

    public int? Month { get; init; }

    public int? Day { get; init; }

    public HistoryDatePrecision DatePrecision { get; init; }

    public string EventType { get; init; } = string.Empty;

    public bool IsMajor { get; init; }

    public bool IsVisible { get; init; }

    public string? Slug { get; init; }

    public List<LocalizedText> Titles { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Summaries { get; init; } = new List<LocalizedText>();

    public string? MainImageId { get; init; }

    public string? PreviousName { get; init; }

    public string? NewName { get; init; }

    public string? PreviousLogoImageId { get; init; }

    public string? NewLogoImageId { get; init; }

    public string? PreviousOperatorId { get; init; }

    public string? NewOperatorId { get; init; }

    public string? LocationLabel { get; init; }

    public List<string> RelatedParkIds { get; init; } = new List<string>();

    public List<string> RelatedParkItemIds { get; init; } = new List<string>();

    public List<ParkGraphExportHistorySource> Sources { get; init; } = new List<ParkGraphExportHistorySource>();

    public ParkGraphExportHistoryArticle? Article { get; init; }
}
