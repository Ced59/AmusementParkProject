using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed record PublicSeoContext(string PublicBaseUrl, IReadOnlyCollection<string> SupportedLanguages);

public sealed class PublicSeoUpdate
{
    public IReadOnlyCollection<PublicSeoParkSnapshot> PreviousParks { get; init; } = Array.Empty<PublicSeoParkSnapshot>();

    public IReadOnlyCollection<PublicSeoParkSnapshot> CurrentParks { get; init; } = Array.Empty<PublicSeoParkSnapshot>();

    public IReadOnlyCollection<PublicSeoParkItemSnapshot> PreviousParkItems { get; init; } = Array.Empty<PublicSeoParkItemSnapshot>();

    public IReadOnlyCollection<PublicSeoParkItemSnapshot> CurrentParkItems { get; init; } = Array.Empty<PublicSeoParkItemSnapshot>();

    public bool IncludeDiscoveryPages { get; init; }
}

public sealed record PublicSeoParkSnapshot(
    string Id,
    string Name,
    bool IsVisible,
    AdminReviewStatus AdminReviewStatus,
    DateTime? UpdatedAtUtc)
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
            park.AdminReviewStatus,
            park.UpdatedAtUtc);
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

public sealed record PublicSeoParkItemSnapshot(
    string Id,
    string ParkId,
    string? ZoneId,
    string Name,
    bool IsVisible,
    AdminReviewStatus AdminReviewStatus,
    DateTime? UpdatedAtUtc)
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
            item.UpdatedAtUtc);
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
