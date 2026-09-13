using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportOpeningHoursTimeRange
{
    public string OpensAt { get; init; } = string.Empty;

    public string ClosesAt { get; init; } = string.Empty;

    public bool ClosesNextDay { get; init; }

    public string? LastAdmissionAt { get; init; }

    public bool LastAdmissionNextDay { get; init; }
}
