using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Handlers;

public sealed class GetPublicSitemapSeedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPublicParkItemExists_ShouldIncludeDetailAndImagesUrlsForEachLanguage()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        DateTime itemUpdatedAtUtc = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc);
        Park parentPark = new Park
        {
            Id = "park-1",
            Name = "Visible Park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            UpdatedAtUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        ParkItem publicItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Attraction familiale",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            UpdatedAtUtc = itemUpdatedAtUtc,
        };
        AttractionManufacturer visibleManufacturer = new AttractionManufacturer
        {
            Id = "manufacturer-visible",
            Name = "Visible Maker",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        AttractionManufacturer hiddenManufacturer = new AttractionManufacturer
        {
            Id = "manufacturer-hidden",
            Name = "Hidden Maker",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOperatorRepository> parkOperatorRepository = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        Mock<IParkFounderRepository> parkFounderRepository = new Mock<IParkFounderRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> attractionManufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository(
            CreateImage("image-park-1", ImageOwnerType.Park, "park-1", ImageCategory.Park),
            CreateImage("image-item-1", ImageOwnerType.ParkItem, "item-1", ImageCategory.ParkItem));
        Mock<IVideoRepository> videoRepository = CreateVideoRepository(
            CreateVideo("video-park-1", VideoOwnerType.Park, "park-1", "Park tour", new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc)),
            CreateVideo("video-item-1", VideoOwnerType.ParkItem, "item-1", "Front row", new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc)));
        SetupPublicSitemapParks(parkRepository, new[] { parentPark });
        SetupPublicSitemapItems(parkItemRepository, new[] { publicItem });
        parkOperatorRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<ParkOperator>());
        parkFounderRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<ParkFounder>());
        attractionManufacturerRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(new[] { visibleManufacturer, hiddenManufacturer });
        GetPublicSitemapSeedQueryHandler handler = new GetPublicSitemapSeedQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            parkOperatorRepository.Object,
            parkFounderRepository.Object,
            attractionManufacturerRepository.Object,
            imageRepository.Object,
            videoRepository.Object);

        ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>> result = await handler.HandleAsync(
            new GetPublicSitemapSeedQuery(new[] { "fr", "en" }),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/rankings");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/rankings");
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/images" && url.LastModifiedUtc == new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale/images" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/videos" && url.LastModifiedUtc == new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/weather" && url.LastModifiedUtc == new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/videos/video-park-1/park-tour" && url.LastModifiedUtc == new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale/videos" && url.LastModifiedUtc == new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale/videos/video-item-1/front-row" && url.LastModifiedUtc == new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park-manufacturer/manufacturer-visible/visible-maker");
        Assert.DoesNotContain(result.Value, static url => url.RelativePath == "/fr/park-manufacturer/manufacturer-hidden/hidden-maker");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/images");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/weather");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale/images");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/videos/video-park-1/park-tour");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale/videos/video-item-1/front-row");
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkOperatorRepository.VerifyAll();
        parkFounderRepository.VerifyAll();
        attractionManufacturerRepository.VerifyAll();
        imageRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenOnlyParkItemHasPublishedImage_ShouldIncludeParentParkImagesUrl()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        Park parentPark = new Park
        {
            Id = "park-1",
            Name = "Visible Park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            UpdatedAtUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        ParkItem publicItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Attraction familiale",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            UpdatedAtUtc = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOperatorRepository> parkOperatorRepository = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        Mock<IParkFounderRepository> parkFounderRepository = new Mock<IParkFounderRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> attractionManufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository(
            CreateImage("image-item-1", ImageOwnerType.ParkItem, "item-1", ImageCategory.ParkItem));
        Mock<IVideoRepository> videoRepository = CreateVideoRepository();
        SetupPublicSitemapParks(parkRepository, new[] { parentPark });
        SetupPublicSitemapItems(parkItemRepository, new[] { publicItem });
        parkOperatorRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<ParkOperator>());
        parkFounderRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<ParkFounder>());
        attractionManufacturerRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<AttractionManufacturer>());
        GetPublicSitemapSeedQueryHandler handler = new GetPublicSitemapSeedQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            parkOperatorRepository.Object,
            parkFounderRepository.Object,
            attractionManufacturerRepository.Object,
            imageRepository.Object,
            videoRepository.Object);

        ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>> result = await handler.HandleAsync(
            new GetPublicSitemapSeedQuery(new[] { "fr" }),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/images" && url.LastModifiedUtc == new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale/images" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkOperatorRepository.VerifyAll();
        parkFounderRepository.VerifyAll();
        attractionManufacturerRepository.VerifyAll();
        imageRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    private static void SetupPublicSitemapParks(Mock<IParkRepository> repository, IReadOnlyCollection<Park> parks)
    {
        repository
            .Setup(item => item.GetPageAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                false,
                true,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.OpenOnly,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                false))
            .Returns((
                int page,
                int pageSize,
                bool includeHidden,
                bool? isVisible,
                AdminReviewStatus? adminReviewStatus,
                ParkType? type,
                string? countryCode,
                bool? hasValidCoordinates,
                ClosedEntityFilter closedFilter,
                CancellationToken cancellationToken,
                ParkAdminSortField sortField,
                bool sortDescending) =>
            {
                IReadOnlyCollection<Park> pageItems = parks
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<Park>(pageItems, page, pageSize, parks.Count));
            });
    }

    private static void SetupPublicSitemapItems(Mock<IParkItemRepository> repository, IReadOnlyCollection<ParkItem> items)
    {
        repository
            .Setup(item => item.GetPageAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                null,
                null,
                false,
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>(),
                ParkItemAdminSortField.ParkId,
                false))
            .Returns((
                int page,
                int pageSize,
                string? parkId,
                string? search,
                bool includeHidden,
                bool? isVisible,
                AdminReviewStatus? adminReviewStatus,
                ParkItemCategory? category,
                ParkItemType? type,
                string? zoneId,
                string? manufacturerId,
                ParkItemContentBacklogFilter? contentBacklogFilter,
                CancellationToken cancellationToken,
                ParkItemAdminSortField sortField,
                bool sortDescending) =>
            {
                IReadOnlyCollection<ParkItem> pageItems = items
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<ParkItem>(pageItems, page, pageSize, items.Count));
            });
    }

    private static Mock<IImageRepository> CreateImageRepository(params Image[] images)
    {
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ImageSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Returns((int page, int pageSize, ImageSearchCriteria criteria, CancellationToken cancellationToken) =>
            {
                int safePage = Math.Max(1, page);
                int safePageSize = Math.Max(1, pageSize);
                List<Image> matchingImages = images
                    .Where(image => MatchesCriteria(image, criteria))
                    .ToList();
                IReadOnlyCollection<Image> pageItems = matchingImages
                    .Skip((safePage - 1) * safePageSize)
                    .Take(safePageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<Image>(pageItems, safePage, safePageSize, matchingImages.Count));
            });

        return repository;
    }

    private static Image CreateImage(string id, ImageOwnerType ownerType, string ownerId, ImageCategory category)
    {
        return new Image
        {
            Id = id,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Category = category,
            IsPublished = true,
        };
    }

    private static Mock<IVideoRepository> CreateVideoRepository(params Video[] videos)
    {
        Mock<IVideoRepository> repository = new Mock<IVideoRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<VideoSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Returns((int page, int pageSize, VideoSearchCriteria criteria, CancellationToken cancellationToken) =>
            {
                int safePage = Math.Max(1, page);
                int safePageSize = Math.Max(1, pageSize);
                List<Video> matchingVideos = videos
                    .Where(video => MatchesCriteria(video, criteria))
                    .ToList();
                IReadOnlyCollection<Video> pageItems = matchingVideos
                    .Skip((safePage - 1) * safePageSize)
                    .Take(safePageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<Video>(pageItems, safePage, safePageSize, matchingVideos.Count));
            });

        return repository;
    }

    private static Video CreateVideo(string id, VideoOwnerType ownerType, string ownerId, string title, DateTime updatedAtUtc)
    {
        return new Video
        {
            Id = id,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Title = title,
            IsPublished = true,
            UpdatedAtUtc = updatedAtUtc,
        };
    }

    private static bool MatchesCriteria(Image image, ImageSearchCriteria criteria)
    {
        if (criteria.Category.HasValue && image.Category != criteria.Category.Value)
        {
            return false;
        }

        if (criteria.OwnerType.HasValue && image.OwnerType != criteria.OwnerType.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.OwnerId) && !string.Equals(image.OwnerId, criteria.OwnerId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (criteria.IsPublished.HasValue && image.IsPublished != criteria.IsPublished.Value)
        {
            return false;
        }

        if (criteria.OwnerIds is not null && !criteria.OwnerIds.Contains(image.OwnerId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (criteria.HasOwner.HasValue)
        {
            bool hasOwner = image.OwnerType != ImageOwnerType.None && !string.IsNullOrWhiteSpace(image.OwnerId);
            if (hasOwner != criteria.HasOwner.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesCriteria(Video video, VideoSearchCriteria criteria)
    {
        if (criteria.OwnerType.HasValue && video.OwnerType != criteria.OwnerType.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.OwnerId) && !string.Equals(video.OwnerId, criteria.OwnerId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (criteria.IsPublished.HasValue && video.IsPublished != criteria.IsPublished.Value)
        {
            return false;
        }

        return true;
    }
}
