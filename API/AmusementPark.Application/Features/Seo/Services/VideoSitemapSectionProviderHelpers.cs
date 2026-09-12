using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Services;

internal static class VideoSitemapSectionProviderHelpers
{
    private const int PublicVideoPageSize = 100;

    public static async Task<IReadOnlyCollection<Video>> LoadPublishedVideosAsync(
        IVideoRepository videoRepository,
        VideoOwnerType ownerType,
        CancellationToken cancellationToken)
    {
        List<Video> videos = new List<Video>();
        int pageNumber = 1;

        while (true)
        {
            VideoSearchCriteria criteria = new VideoSearchCriteria(
                OwnerType: ownerType,
                IsPublished: true,
                SortBy: "updated",
                SortDirection: "desc");
            PagedResult<Video> page = await videoRepository.GetPageAsync(
                pageNumber,
                PublicVideoPageSize,
                criteria,
                cancellationToken);

            videos.AddRange(page.Items.Where(video => IsPublicVideo(video, ownerType)));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return videos;
    }

    public static IReadOnlyDictionary<string, List<Video>> GroupVideosByOwnerId(IReadOnlyCollection<Video> videos)
    {
        Dictionary<string, List<Video>> groupedVideos = new Dictionary<string, List<Video>>(StringComparer.OrdinalIgnoreCase);
        foreach (Video video in videos)
        {
            string ownerId = video.OwnerId!.Trim();
            if (!groupedVideos.TryGetValue(ownerId, out List<Video>? ownerVideos))
            {
                ownerVideos = new List<Video>();
                groupedVideos[ownerId] = ownerVideos;
            }

            ownerVideos.Add(video);
        }

        return groupedVideos;
    }

    public static DateTime? ResolveLatestVideoUpdate(IReadOnlyCollection<Video> videos)
    {
        DateTime? latest = null;
        foreach (Video video in videos)
        {
            if (!latest.HasValue || video.UpdatedAtUtc > latest.Value)
            {
                latest = video.UpdatedAtUtc;
            }
        }

        return latest;
    }

    public static IReadOnlyCollection<string> ResolveVisibleLanguages(Video video, IReadOnlyCollection<string> languages)
    {
        if (video.LanguageCodes.Count == 0)
        {
            return languages;
        }

        HashSet<string> videoLanguages = video.LanguageCodes
            .Where(static languageCode => !string.IsNullOrWhiteSpace(languageCode))
            .Select(static languageCode => NormalizeLanguageCode(languageCode))
            .ToHashSet(StringComparer.Ordinal);

        return languages
            .Where(language => videoLanguages.Contains(NormalizeLanguageCode(language)))
            .ToList();
    }

    public static bool IsVisibleInLanguage(Video video, string language)
    {
        return ResolveVisibleLanguages(video, new[] { language }).Count > 0;
    }

    private static bool IsPublicVideo(Video video, VideoOwnerType ownerType)
    {
        return !string.IsNullOrWhiteSpace(video.Id) &&
               !string.IsNullOrWhiteSpace(video.OwnerId) &&
               video.OwnerType == ownerType &&
               video.IsPublished;
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        return normalizedLanguageCode.Length >= 2 ? normalizedLanguageCode[..2] : normalizedLanguageCode;
    }
}
