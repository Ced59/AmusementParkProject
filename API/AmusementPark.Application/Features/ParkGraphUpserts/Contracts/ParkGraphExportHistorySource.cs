using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportHistorySource
{
    public string? Label { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? AccessedAt { get; init; }
}
