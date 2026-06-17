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

/// <summary>
/// Provider des pages publiques de videos de parcs.
/// </summary>
public sealed class ParkVideosSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IVideoRepository videoRepository;

    public ParkVideosSitemapSectionProvider(IParkRepository parkRepository, IVideoRepository videoRepository)
    {
        this.parkRepository = parkRepository;
        this.videoRepository = videoRepository;
    }

    public string Key => SitemapSectionKeys.ParkVideos;

    public string FileName => "park-videos.xml";

    public string DisplayName => "Videos de parcs";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<Video> publishedVideos = await VideoSitemapSectionProviderHelpers.LoadPublishedVideosAsync(
            this.videoRepository,
            VideoOwnerType.Park,
            cancellationToken);
        IReadOnlyDictionary<string, List<Video>> videosByParkId = VideoSitemapSectionProviderHelpers.GroupVideosByOwnerId(publishedVideos);
        IReadOnlyCollection<string> parentParkIds = videosByParkId.Keys.ToList();

        if (parentParkIds.Count == 0)
        {
            return Array.Empty<SitemapUrlEntry>();
        }

        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(parentParkIds, cancellationToken);
        Dictionary<string, Park> visibleParkById = parentParks
            .Where(static park => ParksSitemapSectionProvider.IsPublicPark(park))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park park in visibleParkById.Values.OrderBy(static park => park.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!videosByParkId.TryGetValue(park.Id!, out List<Video>? parkVideos) || parkVideos.Count == 0)
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
            DateTime? listLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                park.UpdatedAtUtc,
                VideoSitemapSectionProviderHelpers.ResolveLatestVideoUpdate(parkVideos));

            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{parkSlug}/videos", listLastModifiedUtc, "weekly", 0.72m));
            }

            foreach (Video video in parkVideos.OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase))
            {
                string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
                foreach (string language in languages)
                {
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{parkSlug}/videos/{video.Id}/{videoSlug}", video.UpdatedAtUtc, "weekly", 0.66m));
                }
            }
        }

        return urls;
    }
}

/// <summary>
/// Provider des pages publiques de videos d'elements de parc.
/// </summary>
public sealed class ParkItemVideosSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IVideoRepository videoRepository;

    public ParkItemVideosSitemapSectionProvider(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IVideoRepository videoRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.videoRepository = videoRepository;
    }

    public string Key => SitemapSectionKeys.ParkItemVideos;

    public string FileName => "park-item-videos.xml";

    public string DisplayName => "Videos d'elements de parc";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<Video> publishedVideos = await VideoSitemapSectionProviderHelpers.LoadPublishedVideosAsync(
            this.videoRepository,
            VideoOwnerType.ParkItem,
            cancellationToken);
        IReadOnlyDictionary<string, List<Video>> videosByItemId = VideoSitemapSectionProviderHelpers.GroupVideosByOwnerId(publishedVideos);
        IReadOnlyCollection<string> parentItemIds = videosByItemId.Keys.ToList();

        if (parentItemIds.Count == 0)
        {
            return Array.Empty<SitemapUrlEntry>();
        }

        IReadOnlyCollection<ParkItem> candidateItems = await this.parkItemRepository.GetByIdsAsync(parentItemIds, cancellationToken);
        Dictionary<string, ParkItem> publicItemById = candidateItems
            .Where(static item => ParkItemsSitemapSectionProvider.IsPublicItem(item))
            .ToDictionary(static item => item.Id!, static item => item, StringComparer.OrdinalIgnoreCase);
        IReadOnlyCollection<string> parentParkIds = publicItemById.Values
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(parentParkIds, cancellationToken);
        Dictionary<string, Park> visibleParkById = parentParks
            .Where(static park => ParksSitemapSectionProvider.IsPublicPark(park))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> parkSlugById = visibleParkById.ToDictionary(
            static pair => pair.Key,
            static pair => SeoSlugService.ToSlug(pair.Value.Name, "park"),
            StringComparer.OrdinalIgnoreCase);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (ParkItem item in publicItemById.Values.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!visibleParkById.TryGetValue(item.ParkId, out Park? parentPark) ||
                !videosByItemId.TryGetValue(item.Id!, out List<Video>? itemVideos) ||
                itemVideos.Count == 0)
            {
                continue;
            }

            string parkSlug = parkSlugById[item.ParkId];
            string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
            DateTime? listLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                ParkItemListsSitemapSectionProvider.ResolveLatest(parentPark.UpdatedAtUtc, item.UpdatedAtUtc),
                VideoSitemapSectionProviderHelpers.ResolveLatestVideoUpdate(itemVideos));

            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos", listLastModifiedUtc, "weekly", 0.62m));
            }

            foreach (Video video in itemVideos.OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase))
            {
                string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
                foreach (string language in languages)
                {
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos/{video.Id}/{videoSlug}", video.UpdatedAtUtc, "weekly", 0.6m));
                }
            }
        }

        return urls;
    }
}

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

    private static bool IsPublicVideo(Video video, VideoOwnerType ownerType)
    {
        return !string.IsNullOrWhiteSpace(video.Id) &&
               !string.IsNullOrWhiteSpace(video.OwnerId) &&
               video.OwnerType == ownerType &&
               video.IsPublished;
    }
}
