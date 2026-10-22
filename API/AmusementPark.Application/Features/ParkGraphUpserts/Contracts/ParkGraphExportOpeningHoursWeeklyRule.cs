using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportOpeningHoursWeeklyRule
{
    public string? Id { get; init; }

    public List<string> DaysOfWeek { get; init; } = new();

    public bool IsClosed { get; init; }

    public List<LocalizedText> Labels { get; init; } = new();

    public List<LocalizedText> Reasons { get; init; } = new();

    public int SortOrder { get; init; }

    public List<ParkGraphExportOpeningHoursTimeRange> TimeRanges { get; init; } = new();
}
