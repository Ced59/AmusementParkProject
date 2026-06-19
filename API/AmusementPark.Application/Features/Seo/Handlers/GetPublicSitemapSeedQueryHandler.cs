using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Handlers;

/// <summary>
/// Construit un sitemap seed à partir des entités publiques visibles.
/// </summary>
public sealed class GetPublicSitemapSeedQueryHandler : IQueryHandler<GetPublicSitemapSeedQuery, ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>>
{
    private const int PublicSitemapCandidatePageSize = int.MaxValue;

    private static readonly IReadOnlyCollection<string> StaticPublicPages = new[]
    {
        "home",
        "parks",
        "about",
        "contact",
        "versions",
        "privacy",
    };

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly IImageRepository imageRepository;
    private readonly IVideoRepository videoRepository;

    public GetPublicSitemapSeedQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IVideoRepository videoRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.videoRepository = videoRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>> HandleAsync(GetPublicSitemapSeedQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> languages = NormalizeLanguages(query.SupportedLanguages);
        List<PublicSitemapUrl> urls = new List<PublicSitemapUrl>();

        this.AddStaticPages(urls, languages);
        IReadOnlyCollection<Park> visibleParks = await this.GetVisibleParksAsync(cancellationToken);
        Dictionary<string, Park> visibleParkById = visibleParks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        HashSet<string> parkIdsWithPublishedImages = await ParksSitemapSectionProvider.LoadPublishedImageOwnerIdsAsync(
            this.imageRepository,
            ImageOwnerType.Park,
            ImageCategory.Park,
            cancellationToken);
        HashSet<string> itemIdsWithPublishedImages = await ParksSitemapSectionProvider.LoadPublishedImageOwnerIdsAsync(
            this.imageRepository,
            ImageOwnerType.ParkItem,
            ImageCategory.ParkItem,
            cancellationToken);
        IReadOnlyDictionary<string, List<Video>> videosByParkId = VideoSitemapSectionProviderHelpers.GroupVideosByOwnerId(
            await VideoSitemapSectionProviderHelpers.LoadPublishedVideosAsync(
                this.videoRepository,
                VideoOwnerType.Park,
                cancellationToken));
        IReadOnlyDictionary<string, List<Video>> videosByItemId = VideoSitemapSectionProviderHelpers.GroupVideosByOwnerId(
            await VideoSitemapSectionProviderHelpers.LoadPublishedVideosAsync(
                this.videoRepository,
                VideoOwnerType.ParkItem,
                cancellationToken));

        this.AddParkUrls(urls, visibleParks, languages, parkIdsWithPublishedImages, videosByParkId);
        await this.AddParkItemUrlsAsync(urls, visibleParkById, languages, itemIdsWithPublishedImages, videosByItemId, cancellationToken);

        await this.AddReferenceUrlsAsync(urls, languages, cancellationToken);

        IReadOnlyCollection<PublicSitemapUrl> distinctUrls = urls
            .GroupBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.OrderByDescending(url => url.LastModifiedUtc).First())
            .OrderBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>.Success(distinctUrls);
    }

    private static IReadOnlyCollection<string> NormalizeLanguages(IReadOnlyCollection<string> languages)
    {
        List<string> normalizedLanguages = languages
            .Select(static language => language.Trim().ToLowerInvariant())
            .Where(static language => !string.IsNullOrWhiteSpace(language))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedLanguages.Count > 0 ? normalizedLanguages : new[] { "en" };
    }

    private void AddStaticPages(List<PublicSitemapUrl> urls, IReadOnlyCollection<string> languages)
    {
        foreach (string language in languages)
        {
            foreach (string page in StaticPublicPages)
            {
                urls.Add(new PublicSitemapUrl($"/{language}/{page}", null));
            }
        }
    }

    private async Task<IReadOnlyCollection<Park>> GetVisibleParksAsync(CancellationToken cancellationToken)
    {
        PagedResult<Park> page = await this.parkRepository.GetPageAsync(
            1,
            PublicSitemapCandidatePageSize,
            includeHidden: false,
            isVisible: true,
            adminReviewStatus: null,
            type: null,
            countryCode: null,
            hasValidCoordinates: null,
            cancellationToken);

        return page.Items
            .Where(static park => park.IsVisible && park.AdminReviewStatus != AdminReviewStatus.NotRelevant)
            .ToList();
    }

    private void AddParkUrls(
        List<PublicSitemapUrl> urls,
        IReadOnlyCollection<Park> parks,
        IReadOnlyCollection<string> languages,
        HashSet<string> parkIdsWithPublishedImages,
        IReadOnlyDictionary<string, List<Video>> videosByParkId)
    {
        foreach (Park park in parks)
        {
            if (string.IsNullOrWhiteSpace(park.Id))
            {
                continue;
            }

            string slug = SeoSlugService.ToSlug(park.Name, "park");
            foreach (string language in languages)
            {
                urls.Add(new PublicSitemapUrl($"/{language}/park/{park.Id}/{slug}", park.UpdatedAtUtc));
                urls.Add(new PublicSitemapUrl($"/{language}/park/{park.Id}/{slug}/weather", park.UpdatedAtUtc));
                if (parkIdsWithPublishedImages.Contains(park.Id))
                {
                    urls.Add(new PublicSitemapUrl($"/{language}/park/{park.Id}/{slug}/images", park.UpdatedAtUtc));
                }

                if (videosByParkId.TryGetValue(park.Id, out List<Video>? parkVideos) && parkVideos.Count > 0)
                {
                    DateTime? videoListLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                        park.UpdatedAtUtc,
                        VideoSitemapSectionProviderHelpers.ResolveLatestVideoUpdate(parkVideos));
                    urls.Add(new PublicSitemapUrl($"/{language}/park/{park.Id}/{slug}/videos", videoListLastModifiedUtc));
                    foreach (Video video in parkVideos.OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase))
                    {
                        string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
                        urls.Add(new PublicSitemapUrl($"/{language}/park/{park.Id}/{slug}/videos/{video.Id}/{videoSlug}", video.UpdatedAtUtc));
                    }
                }
            }
        }
    }

    private async Task AddParkItemUrlsAsync(
        List<PublicSitemapUrl> urls,
        IReadOnlyDictionary<string, Park> visibleParkById,
        IReadOnlyCollection<string> languages,
        HashSet<string> itemIdsWithPublishedImages,
        IReadOnlyDictionary<string, List<Video>> videosByItemId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetPublicSitemapCandidatesAsync(
            PublicSitemapCandidatePageSize,
            cancellationToken);

        foreach (ParkItem item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !item.IsVisible || item.AdminReviewStatus == AdminReviewStatus.NotRelevant)
            {
                continue;
            }

            if (!visibleParkById.TryGetValue(item.ParkId, out Park? parentPark) || string.IsNullOrWhiteSpace(parentPark.Id))
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(parentPark.Name, "park");
            string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
            foreach (string language in languages)
            {
                urls.Add(new PublicSitemapUrl($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}", item.UpdatedAtUtc));
                if (itemIdsWithPublishedImages.Contains(item.Id))
                {
                    urls.Add(new PublicSitemapUrl($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/images", item.UpdatedAtUtc));
                }

                if (videosByItemId.TryGetValue(item.Id, out List<Video>? itemVideos) && itemVideos.Count > 0)
                {
                    DateTime? itemAndParkLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                        parentPark.UpdatedAtUtc,
                        item.UpdatedAtUtc);
                    DateTime? videoListLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                        itemAndParkLastModifiedUtc,
                        VideoSitemapSectionProviderHelpers.ResolveLatestVideoUpdate(itemVideos));
                    urls.Add(new PublicSitemapUrl($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos", videoListLastModifiedUtc));
                    foreach (Video video in itemVideos.OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase))
                    {
                        string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
                        urls.Add(new PublicSitemapUrl($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos/{video.Id}/{videoSlug}", video.UpdatedAtUtc));
                    }
                }
            }
        }
    }

    private async Task AddReferenceUrlsAsync(List<PublicSitemapUrl> urls, IReadOnlyCollection<string> languages, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkOperator> operators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        IReadOnlyCollection<ParkFounder> founders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);

        foreach (ParkOperator entity in operators.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant))
        {
            this.AddReferenceUrls(urls, languages, "park-operator", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        foreach (ParkFounder entity in founders)
        {
            this.AddReferenceUrls(urls, languages, "park-founder", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        foreach (AttractionManufacturer entity in manufacturers.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant))
        {
            this.AddReferenceUrls(urls, languages, "park-manufacturer", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }
    }

    private void AddReferenceUrls(List<PublicSitemapUrl> urls, IReadOnlyCollection<string> languages, string routeSegment, string? id, string? name, DateTime? lastModifiedUtc)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string slug = SeoSlugService.ToSlug(name, "reference");
        foreach (string language in languages)
        {
            urls.Add(new PublicSitemapUrl($"/{language}/{routeSegment}/{id}/{slug}", lastModifiedUtc));
        }
    }
}
