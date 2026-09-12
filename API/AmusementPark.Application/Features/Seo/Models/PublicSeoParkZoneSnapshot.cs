using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed record PublicSeoParkZoneSnapshot(
    string Id,
    string ParkId,
    string Name,
    bool IsVisible)
{
    public static PublicSeoParkZoneSnapshot? FromParkZone(ParkZone? zone)
    {
        if (zone is null || string.IsNullOrWhiteSpace(zone.Id) || string.IsNullOrWhiteSpace(zone.ParkId))
        {
            return null;
        }

        return new PublicSeoParkZoneSnapshot(
            zone.Id.Trim(),
            zone.ParkId.Trim(),
            zone.Name ?? string.Empty,
            zone.IsVisible);
    }

    public static IReadOnlyCollection<PublicSeoParkZoneSnapshot> FromParkZones(IEnumerable<ParkZone?> zones)
    {
        ArgumentNullException.ThrowIfNull(zones);

        List<PublicSeoParkZoneSnapshot> snapshots = new List<PublicSeoParkZoneSnapshot>();
        foreach (ParkZone? zone in zones)
        {
            PublicSeoParkZoneSnapshot? snapshot = FromParkZone(zone);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }
}
