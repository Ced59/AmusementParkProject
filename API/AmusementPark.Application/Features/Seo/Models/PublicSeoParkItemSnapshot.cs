using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed record PublicSeoParkItemSnapshot(
    string Id,
    string ParkId,
    string? ZoneId,
    string Name,
    bool IsVisible,
    AdminReviewStatus AdminReviewStatus,
    DateTime? UpdatedAtUtc,
    string? Status = null,
    DateTime? OpeningDate = null,
    DateTime? ClosingDate = null,
    bool HasPosition = false,
    string? OpeningDateText = null,
    string? ClosingDateText = null)
{
    public static PublicSeoParkItemSnapshot? FromParkItem(ParkItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.ParkId))
        {
            return null;
        }

        return new PublicSeoParkItemSnapshot(
            item.Id.Trim(),
            item.ParkId.Trim(),
            string.IsNullOrWhiteSpace(item.ZoneId) ? null : item.ZoneId.Trim(),
            item.Name ?? string.Empty,
            item.IsVisible,
            item.AdminReviewStatus,
            item.UpdatedAtUtc,
            item.AttractionDetails?.Status,
            item.AttractionDetails?.OpeningDate,
            item.AttractionDetails?.ClosingDate,
            item.Position is not null
                && !(Math.Abs(item.Position.Latitude) < double.Epsilon && Math.Abs(item.Position.Longitude) < double.Epsilon),
            item.AttractionDetails?.OpeningDateText,
            item.AttractionDetails?.ClosingDateText);
    }

    public static IReadOnlyCollection<PublicSeoParkItemSnapshot> FromParkItems(IEnumerable<ParkItem?> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<PublicSeoParkItemSnapshot> snapshots = new List<PublicSeoParkItemSnapshot>();
        foreach (ParkItem? item in items)
        {
            PublicSeoParkItemSnapshot? snapshot = FromParkItem(item);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }
}
