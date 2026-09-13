using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportOpeningHoursRule
{
    public string? Id { get; init; }

    public string StartDate { get; init; } = string.Empty;

    public string EndDate { get; init; } = string.Empty;

    public List<string> DaysOfWeek { get; init; } = new List<string>();

    public bool IsClosed { get; init; }

    public List<LocalizedText> Labels { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Reasons { get; init; } = new List<LocalizedText>();

    public int SortOrder { get; init; }

    public List<ParkGraphExportOpeningHoursTimeRange> TimeRanges { get; init; } = new List<ParkGraphExportOpeningHoursTimeRange>();
}
