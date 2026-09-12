using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.History.Contracts;

public sealed class HistoryEventWriteModel
{
    public string? Id { get; init; }

    public string? Key { get; init; }

    public HistoryEntityType EntityType { get; init; }

    public string? OwnerId { get; init; }

    public string? ParkId { get; init; }

    public string? ParkItemId { get; init; }

    public string? ContextParkId { get; init; }

    public int Year { get; init; }

    public int? Month { get; init; }

    public int? Day { get; init; }

    public HistoryDatePrecision DatePrecision { get; init; } = HistoryDatePrecision.Year;

    public string EventType { get; init; } = string.Empty;

    public bool IsMajor { get; init; }

    public bool IsVisible { get; init; } = true;

    public string? Slug { get; init; }

    public IReadOnlyCollection<LocalizedText> Titles { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<LocalizedText> Summaries { get; init; } = Array.Empty<LocalizedText>();

    public string? MainImageId { get; init; }

    public string? PreviousName { get; init; }

    public string? NewName { get; init; }

    public string? PreviousLogoImageId { get; init; }

    public string? NewLogoImageId { get; init; }

    public string? PreviousOperatorId { get; init; }

    public string? NewOperatorId { get; init; }

    public string? LocationLabel { get; init; }

    public IReadOnlyCollection<string> RelatedParkIds { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> RelatedParkItemIds { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<HistorySourceReference> Sources { get; init; } = Array.Empty<HistorySourceReference>();

    public HistoryArticle? Article { get; init; }
}
