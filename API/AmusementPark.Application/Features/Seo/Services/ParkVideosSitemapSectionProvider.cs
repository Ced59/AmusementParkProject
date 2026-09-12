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
    private readonly IParkItemRepository parkItemRepository;
    private readonly IVideoRepository videoRepository;

    public ParkVideosSitemapSectionProvider(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IVideoRepository videoRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
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
        IReadOnlyCollection<Video> publishedItemVideos = await VideoSitemapSectionProviderHelpers.LoadPublishedVideosAsync(
            this.videoRepository,
            VideoOwnerType.ParkItem,
            cancellationToken);
        IReadOnlyDictionary<string, List<Video>> videosByItemId = VideoSitemapSectionProviderHelpers.GroupVideosByOwnerId(publishedItemVideos);
        IReadOnlyCollection<ParkItem> candidateItems = videosByItemId.Count == 0
            ? Array.Empty<ParkItem>()
            : await this.parkItemRepository.GetByIdsAsync(videosByItemId.Keys.ToList(), cancellationToken);
        IReadOnlyDictionary<string, List<Video>> itemVideosByParkId = GroupPublicItemVideosByParkId(candidateItems, videosByItemId);
        IReadOnlyCollection<string> parentParkIds = videosByParkId.Keys
            .Concat(itemVideosByParkId.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

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
            List<Video> parkVideos = videosByParkId.GetValueOrDefault(park.Id!) ?? new List<Video>();
            List<Video> itemVideos = itemVideosByParkId.GetValueOrDefault(park.Id!) ?? new List<Video>();
            if (parkVideos.Count == 0 && itemVideos.Count == 0)
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
            DateTime? listLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                park.UpdatedAtUtc,
                VideoSitemapSectionProviderHelpers.ResolveLatestVideoUpdate(parkVideos));
            listLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                listLastModifiedUtc,
                VideoSitemapSectionProviderHelpers.ResolveLatestVideoUpdate(itemVideos));

            foreach (string language in languages)
            {
                int visibleVideoCount = parkVideos.Count(video => VideoSitemapSectionProviderHelpers.IsVisibleInLanguage(video, language)) +
                                        itemVideos.Count(video => VideoSitemapSectionProviderHelpers.IsVisibleInLanguage(video, language));
                if (SeoPageValuePolicy.IsCollectionIndexable(visibleVideoCount))
                {
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{parkSlug}/videos", listLastModifiedUtc, "weekly", 0.72m));
                }
            }

            foreach (Video video in parkVideos.OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase))
            {
                string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
                foreach (string language in VideoSitemapSectionProviderHelpers.ResolveVisibleLanguages(video, languages))
                {
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{parkSlug}/videos/{video.Id}/{videoSlug}", video.UpdatedAtUtc, "weekly", 0.66m));
                }
            }
        }

        return urls;
    }

    private static IReadOnlyDictionary<string, List<Video>> GroupPublicItemVideosByParkId(
        IReadOnlyCollection<ParkItem> candidateItems,
        IReadOnlyDictionary<string, List<Video>> videosByItemId)
    {
        Dictionary<string, List<Video>> videosByParkId = new Dictionary<string, List<Video>>(StringComparer.OrdinalIgnoreCase);
        foreach (ParkItem item in candidateItems.Where(static item => ParkItemsSitemapSectionProvider.IsPublicItem(item)))
        {
            if (string.IsNullOrWhiteSpace(item.Id) ||
                string.IsNullOrWhiteSpace(item.ParkId) ||
                !videosByItemId.TryGetValue(item.Id, out List<Video>? itemVideos))
            {
                continue;
            }

            if (!videosByParkId.TryGetValue(item.ParkId, out List<Video>? parkVideos))
            {
                parkVideos = new List<Video>();
                videosByParkId[item.ParkId] = parkVideos;
            }

            parkVideos.AddRange(itemVideos);
        }

        return videosByParkId;
    }
}
