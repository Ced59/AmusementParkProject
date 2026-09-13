using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

/// <summary>
/// Contrat HTTP optimisé pour la carte détaillée publique d'un parc.
/// </summary>
public sealed class ParkMapItemsDto
{
    public required ParkDto Park { get; set; }

    public IReadOnlyCollection<ParkMapItemDto> Items { get; set; } = Array.Empty<ParkMapItemDto>();

    public IReadOnlyCollection<ParkMapUnlocatedItemDto> UnlocatedItems { get; set; } = Array.Empty<ParkMapUnlocatedItemDto>();

    public IReadOnlyCollection<ParkMapZoneDto> Zones { get; set; } = Array.Empty<ParkMapZoneDto>();

    public IReadOnlyCollection<ParkOfficialMapDto> OfficialMaps { get; set; } = Array.Empty<ParkOfficialMapDto>();
}
