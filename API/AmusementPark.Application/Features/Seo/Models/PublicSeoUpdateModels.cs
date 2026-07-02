using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed record PublicSeoContext(string PublicBaseUrl, IReadOnlyCollection<string> SupportedLanguages);

public sealed class PublicSeoUpdate
{
    public IReadOnlyCollection<PublicSeoParkSnapshot> PreviousParks { get; init; } = Array.Empty<PublicSeoParkSnapshot>();

    public IReadOnlyCollection<PublicSeoParkSnapshot> CurrentParks { get; init; } = Array.Empty<PublicSeoParkSnapshot>();

    public IReadOnlyCollection<PublicSeoParkItemSnapshot> PreviousParkItems { get; init; } = Array.Empty<PublicSeoParkItemSnapshot>();

    public IReadOnlyCollection<PublicSeoParkItemSnapshot> CurrentParkItems { get; init; } = Array.Empty<PublicSeoParkItemSnapshot>();

    public IReadOnlyCollection<PublicSeoVideoSnapshot> PreviousVideos { get; init; } = Array.Empty<PublicSeoVideoSnapshot>();

    public IReadOnlyCollection<PublicSeoVideoSnapshot> CurrentVideos { get; init; } = Array.Empty<PublicSeoVideoSnapshot>();

    public bool IncludeDiscoveryPages { get; init; }

    public bool SuppressSitemapRefresh { get; init; }
}

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
    string? ClosingDateText = null)
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
            park.ClosingDateText);
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

public sealed record PublicSeoVideoSnapshot(
    string Id,
    VideoOwnerType OwnerType,
    string OwnerId,
    string Title,
    bool IsPublished,
    DateTime? UpdatedAtUtc)
{
    public IReadOnlyCollection<string> LanguageCodes { get; init; } = Array.Empty<string>();

    public static PublicSeoVideoSnapshot? FromVideo(Video? video)
    {
        if (video is null || string.IsNullOrWhiteSpace(video.Id) || string.IsNullOrWhiteSpace(video.OwnerId))
        {
            return null;
        }

        return new PublicSeoVideoSnapshot(
            video.Id.Trim(),
            video.OwnerType,
            video.OwnerId.Trim(),
            video.Title ?? string.Empty,
            video.IsPublished,
            video.UpdatedAtUtc)
        {
            LanguageCodes = video.LanguageCodes
                .Where(static languageCode => !string.IsNullOrWhiteSpace(languageCode))
                .Select(static languageCode => languageCode.Trim().ToLowerInvariant())
                .ToList(),
        };
    }

    public static IReadOnlyCollection<PublicSeoVideoSnapshot> FromVideos(IEnumerable<Video?> videos)
    {
        ArgumentNullException.ThrowIfNull(videos);

        List<PublicSeoVideoSnapshot> snapshots = new List<PublicSeoVideoSnapshot>();
        foreach (Video? video in videos)
        {
            PublicSeoVideoSnapshot? snapshot = FromVideo(video);
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
