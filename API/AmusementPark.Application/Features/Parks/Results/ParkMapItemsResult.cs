using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Résultat optimisé pour la page carte d'un parc.
/// </summary>
public sealed class ParkMapItemsResult
{
    public required Park Park { get; init; }

    public IReadOnlyCollection<ParkMapItemResult> Items { get; init; } = Array.Empty<ParkMapItemResult>();

    public IReadOnlyCollection<ParkMapUnlocatedItemResult> UnlocatedItems { get; init; } = Array.Empty<ParkMapUnlocatedItemResult>();

    public IReadOnlyCollection<ParkMapZoneResult> Zones { get; init; } = Array.Empty<ParkMapZoneResult>();

    public IReadOnlyCollection<ParkOfficialMap> OfficialMaps { get; init; } = Array.Empty<ParkOfficialMap>();
}
