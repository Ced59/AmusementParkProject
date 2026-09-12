using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

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
