using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportOpeningHours
{
    public string ParkId { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public string? Notes { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public List<ParkGraphExportOpeningHoursRule> RegularRules { get; init; } = new List<ParkGraphExportOpeningHoursRule>();

    public List<ParkGraphExportOpeningHoursDateOverride> DateOverrides { get; init; } = new List<ParkGraphExportOpeningHoursDateOverride>();
}
