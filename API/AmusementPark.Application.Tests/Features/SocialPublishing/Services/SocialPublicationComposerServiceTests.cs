using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Services;

public sealed class SocialPublicationComposerServiceTests
{
    [Fact]
    public async Task ResolveDraftAsync_ForPark_ShouldUseAutomaticMessageAndPageEligibleImages()
    {
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetPageAsync(
                1,
                1,
                It.Is<ImageSearchCriteria>(criteria =>
                    criteria.OwnerType == ImageOwnerType.Park
                    && criteria.OwnerId == "park-1"
                    && criteria.Category == ImageCategory.Park
                    && criteria.IsPublished == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(
                new[] { CreateImage("image-current", "park-1", true, true) },
                1,
                1,
                2));
        SocialPublicationComposerService service = CreateService(parks, images);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test",
            1,
            1,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal(SocialPublicationTargetKind.Park, draft.TargetKind);
        Assert.Equal(SocialPublicationService.BuildParkAnnouncementMessage("Parc Test"), draft.DefaultMessage);
        Assert.Equal(2, draft.Images.TotalItems);
        SocialPublicationImageOption image = Assert.Single(draft.Images.Items);
        Assert.Equal("image-current", image.Id);
        Assert.True(image.IsCurrent);
        images.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForParkItem_ShouldUseItemNameAndOnlyItemImages()
    {
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Grand Huit",
                IsVisible = true,
            });
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetPageAsync(
                1,
                6,
                It.Is<ImageSearchCriteria>(criteria =>
                    criteria.OwnerType == ImageOwnerType.ParkItem
                    && criteria.OwnerId == "item-1"
                    && criteria.Category == ImageCategory.ParkItem
                    && criteria.IsPublished == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(
                new[] { CreateImage("item-image", "item-1", true, true, ImageOwnerType.ParkItem, ImageCategory.ParkItem) },
                1,
                6,
                1));
        SocialPublicationComposerService service = CreateService(parks, images, items);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test/item/item-1/grand-huit",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal(SocialPublicationTargetKind.ParkItem, draft.TargetKind);
        Assert.Equal("Grand Huit", draft.TargetName);
        Assert.Contains("Grand Huit", draft.DefaultMessage, StringComparison.Ordinal);
        Assert.Equal("item-image", Assert.Single(draft.Images.Items).Id);
        items.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task PublishAsync_WithEligibleImageAndNoMessage_ShouldUseDefaultTextAndImageQuery()
    {
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetByIdAsync("image-current", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateImage("image-current", "park-1", true, true));
        Mock<ISocialPublicationService> publisher = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        SocialPublication published = new SocialPublication { Id = "publication-1" };
        publisher.Setup(service => service.PublishManualAsync(
                It.Is<SocialLinkPublicationRequest>(request =>
                    request.Message == SocialPublicationService.BuildParkAnnouncementMessage("Parc Test")
                    && request.Url == "https://amusement-parks.fun/fr/park/park-1/park-test?facebook-image=image-current"
                    && request.PreviewImageId == null),
                "codex-user",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(published));
        SocialPublicationComposerService service = CreateService(parks, images, publisher: publisher);

        ApplicationResult<SocialPublication> result = await service.PublishAsync(
            new SocialLinkPublicationRequest(
                SocialNetwork.Facebook,
                null,
                "https://amusement-parks.fun/fr/park/park-1/park-test",
                "image-current"),
            "codex-user",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(published, result.Value);
        images.VerifyAll();
        publisher.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForVideo_ShouldUseLocalizedVideoTitle()
    {
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IVideoRepository> videos = new Mock<IVideoRepository>(MockBehavior.Strict);
        videos.Setup(repository => repository.GetByIdAsync("video-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Video
            {
                Id = "video-1",
                OwnerType = VideoOwnerType.Park,
                OwnerId = "park-1",
                Title = "Fallback title",
                Titles = new List<LocalizedText>
                {
                    new LocalizedText("fr", "Visite du parc"),
                    new LocalizedText("en", "Park tour"),
                },
                IsPublished = true,
            });
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetPageAsync(
                1,
                6,
                It.Is<ImageSearchCriteria>(criteria => criteria.OwnerId == "park-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(Array.Empty<Image>(), 1, 6, 0));
        SocialPublicationComposerService service = CreateService(parks, images, videos: videos);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test/videos/video-1/visite-du-parc",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal(SocialPublicationTargetKind.Video, draft.TargetKind);
        Assert.Equal("Visite du parc", draft.TargetName);
        Assert.Contains("Park tour", draft.DefaultMessage, StringComparison.Ordinal);
        videos.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task PublishAsync_WithForeignImage_ShouldRejectBeforePublishing()
    {
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetByIdAsync("foreign-image", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateImage("foreign-image", "park-2", true, false));
        Mock<ISocialPublicationService> publisher = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        SocialPublicationComposerService service = CreateService(parks, images, publisher: publisher);

        ApplicationResult<SocialPublication> result = await service.PublishAsync(
            new SocialLinkPublicationRequest(
                SocialNetwork.Facebook,
                "Texte personnalisé",
                "https://amusement-parks.fun/fr/park/park-1/park-test",
                "foreign-image"),
            "codex-user",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "social-publishing.preview-image.invalid");
        publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForPublicStaticPage_ShouldReturnTextWithoutImages()
    {
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            new Mock<IImageRepository>(MockBehavior.Strict));

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/privacy",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal(SocialPublicationTargetKind.Page, draft.TargetKind);
        Assert.Equal(0, draft.Images.TotalItems);
    }

    private static SocialPublicationComposerService CreateService(
        Mock<IParkRepository> parks,
        Mock<IImageRepository> images,
        Mock<IParkItemRepository>? items = null,
        Mock<ISocialPublicationService>? publisher = null,
        Mock<IVideoRepository>? videos = null)
    {
        Mock<IPublicSeoContextProvider> seoContext = new Mock<IPublicSeoContextProvider>(MockBehavior.Strict);
        seoContext.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicSeoContext("https://amusement-parks.fun", new[] { "de", "en", "es", "fr", "it", "nl", "pl", "pt" }));
        SocialPublicationTargetResolver targetResolver = new SocialPublicationTargetResolver(
            seoContext.Object,
            parks.Object,
            items?.Object ?? Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            videos?.Object ?? Mock.Of<IVideoRepository>(MockBehavior.Strict));
        return new SocialPublicationComposerService(
            publisher?.Object ?? Mock.Of<ISocialPublicationService>(MockBehavior.Strict),
            targetResolver,
            images.Object);
    }

    private static Mock<IParkRepository> CreateParkRepository()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park
            {
                Id = "park-1",
                Name = "Parc Test",
                IsVisible = true,
            });
        return parks;
    }

    private static Image CreateImage(
        string id,
        string ownerId,
        bool isPublished,
        bool isCurrent,
        ImageOwnerType ownerType = ImageOwnerType.Park,
        ImageCategory category = ImageCategory.Park)
    {
        return new Image
        {
            Id = id,
            OwnerId = ownerId,
            OwnerType = ownerType,
            Category = category,
            IsPublished = isPublished,
            IsCurrent = isCurrent,
            Width = 1200,
            Height = 630,
            Captions = new List<LocalizedText> { new LocalizedText("fr", $"Légende {id}") },
        };
    }
}
