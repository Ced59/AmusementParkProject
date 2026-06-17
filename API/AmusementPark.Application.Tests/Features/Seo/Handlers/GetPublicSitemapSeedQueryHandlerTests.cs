using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
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
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOperatorRepository> parkOperatorRepository = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        Mock<IParkFounderRepository> parkFounderRepository = new Mock<IParkFounderRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> attractionManufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository(
            CreateImage("image-park-1", ImageOwnerType.Park, "park-1", ImageCategory.Park),
            CreateImage("image-item-1", ImageOwnerType.ParkItem, "item-1", ImageCategory.ParkItem));
        parkRepository
            .Setup(repository => repository.GetPageAsync(1, int.MaxValue, false, true, null, null, null, null, cancellationToken))
            .ReturnsAsync(new PagedResult<Park>(new[] { parentPark }, 1, int.MaxValue, 1));
        parkItemRepository
            .Setup(repository => repository.GetPublicSitemapCandidatesAsync(int.MaxValue, cancellationToken))
            .ReturnsAsync(new[] { publicItem });
        parkOperatorRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<ParkOperator>());
        parkFounderRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<ParkFounder>());
        attractionManufacturerRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync(Array.Empty<AttractionManufacturer>());
        GetPublicSitemapSeedQueryHandler handler = new GetPublicSitemapSeedQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            parkOperatorRepository.Object,
            parkFounderRepository.Object,
            attractionManufacturerRepository.Object,
            imageRepository.Object);

        ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>> result = await handler.HandleAsync(
            new GetPublicSitemapSeedQuery(new[] { "fr", "en" }),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale/images" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale");
        Assert.Contains(result.Value, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale/images");
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkOperatorRepository.VerifyAll();
        parkFounderRepository.VerifyAll();
        attractionManufacturerRepository.VerifyAll();
        imageRepository.VerifyAll();
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

        if (criteria.IsPublished.HasValue && image.IsPublished != criteria.IsPublished.Value)
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
}
