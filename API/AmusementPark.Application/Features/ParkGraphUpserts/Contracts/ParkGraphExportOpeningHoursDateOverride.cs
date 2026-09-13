using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportOpeningHoursDateOverride
{
    public string LocalDate { get; init; } = string.Empty;

    public bool IsClosed { get; init; }

    public List<LocalizedText> Labels { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Reasons { get; init; } = new List<LocalizedText>();

    public List<ParkGraphExportOpeningHoursTimeRange> TimeRanges { get; init; } = new List<ParkGraphExportOpeningHoursTimeRange>();
}
