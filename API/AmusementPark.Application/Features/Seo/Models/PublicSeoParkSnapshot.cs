using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed record PublicSeoParkSnapshot(
    string Id,
    string Name,
    bool IsVisible,
    ParkStatus Status,
    AdminReviewStatus AdminReviewStatus,
    DateTime? UpdatedAtUtc,
    DateTime? OpeningDate = null,
    DateTime? ClosingDate = null,
    string? OpeningDateText = null,
    string? ClosingDateText = null,
    bool HasPublicOfficialMaps = false)
{
    public static PublicSeoParkSnapshot? FromPark(Park? park)
    {
        if (park is null || string.IsNullOrWhiteSpace(park.Id))
        {
            return null;
        }

        return new PublicSeoParkSnapshot(
            park.Id.Trim(),
            park.Name ?? string.Empty,
            park.IsVisible,
            park.Status,
            park.AdminReviewStatus,
            park.UpdatedAtUtc,
            park.OpeningDate,
            park.ClosingDate,
            park.OpeningDateText,
            park.ClosingDateText,
            park.HasPublicOfficialMaps());
    }

    public static IReadOnlyCollection<PublicSeoParkSnapshot> FromParks(IEnumerable<Park?> parks)
    {
        ArgumentNullException.ThrowIfNull(parks);

        List<PublicSeoParkSnapshot> snapshots = new List<PublicSeoParkSnapshot>();
        foreach (Park? park in parks)
        {
            PublicSeoParkSnapshot? snapshot = FromPark(park);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }
}
