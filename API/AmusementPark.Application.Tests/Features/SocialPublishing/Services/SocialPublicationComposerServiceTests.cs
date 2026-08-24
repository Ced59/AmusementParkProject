using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Core.Domain.TechnicalPages;
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
        Assert.Equal(
            SocialPublicationMessageBuilder.BuildParkAnnouncementMessage(
                "Parc Test",
                "Parc Test",
                new Uri("https://amusement-parks.fun/fr/park/park-1/park-test")),
            draft.DefaultMessage);
        Assert.Equal(2, draft.Images.TotalItems);
        Assert.False(draft.HasPublishedParkAnnouncement);
        Assert.Null(draft.ParkAnnouncementId);
        Assert.Null(draft.ParkAnnouncementStatus);
        SocialPublicationImageOption image = Assert.Single(draft.Images.Items);
        Assert.Equal("image-current", image.Id);
        Assert.True(image.IsCurrent);
        images.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForParkWithPublishedAnnouncement_ShouldExposeExistingPost()
    {
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetPageAsync(
                1,
                6,
                It.IsAny<ImageSearchCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(Array.Empty<Image>(), 1, 6, 0));
        Mock<ISocialPublicationService> publisher = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        publisher.Setup(service => service.GetParkAnnouncementAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SocialPublication
            {
                Id = "publication-1",
                Status = SocialPublicationStatus.Published,
                ExternalPostId = "facebook-post-1",
                ExternalPostUrl = "https://www.facebook.com/test/posts/facebook-post-1",
            });
        SocialPublicationComposerService service = CreateService(parks, images, publisher: publisher);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.True(draft.HasPublishedParkAnnouncement);
        Assert.Equal("publication-1", draft.ParkAnnouncementId);
        Assert.Equal(SocialPublicationStatus.Published, draft.ParkAnnouncementStatus);
        Assert.Equal("https://www.facebook.com/test/posts/facebook-post-1", draft.ParkAnnouncementExternalUrl);
        publisher.VerifyAll();
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
    public async Task ResolveDraftAsync_ForStandaloneAttraction_ShouldUseAttractionNameAndOnlyItsImages()
    {
        Mock<IStandaloneAttractionRepository> standaloneAttractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneAttractions.Setup(repository => repository.GetByIdAsync(
                "standalone-1",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StandaloneAttraction
            {
                Id = "standalone-1",
                Name = "Pendolino",
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
            });
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetPageAsync(
                1,
                6,
                It.Is<ImageSearchCriteria>(criteria =>
                    criteria.OwnerType == ImageOwnerType.StandaloneAttraction
                    && criteria.OwnerId == "standalone-1"
                    && criteria.Category == ImageCategory.StandaloneAttraction
                    && criteria.IsPublished == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(
                new[]
                {
                    CreateImage(
                        "standalone-image",
                        "standalone-1",
                        true,
                        true,
                        ImageOwnerType.StandaloneAttraction,
                        ImageCategory.StandaloneAttraction),
                },
                1,
                6,
                1));
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            images,
            standaloneAttractions: standaloneAttractions);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/attraction/standalone-1/pendolino",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal(SocialPublicationTargetKind.StandaloneAttraction, draft.TargetKind);
        Assert.Equal("Pendolino", draft.TargetName);
        Assert.Contains("Une nouvelle attraction", draft.DefaultMessage, StringComparison.Ordinal);
        Assert.Equal("standalone-image", Assert.Single(draft.Images.Items).Id);
        standaloneAttractions.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task PublishAsync_ForStandaloneAttractionWithoutOverrides_ShouldPublishDefaultMessage()
    {
        Mock<IStandaloneAttractionRepository> standaloneAttractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneAttractions.Setup(repository => repository.GetByIdAsync(
                "standalone-1",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StandaloneAttraction
            {
                Id = "standalone-1",
                Name = "Pendolino",
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
            });
        Mock<ISocialPublicationService> publisher = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        SocialPublication published = new SocialPublication { Id = "publication-standalone" };
        publisher.Setup(service => service.PublishManualAsync(
                It.Is<SocialLinkPublicationRequest>(request =>
                    request.Message != null
                    && request.Message.Contains("Une nouvelle attraction", StringComparison.Ordinal)
                    && request.Url == "https://amusement-parks.fun/fr/attraction/standalone-1/pendolino"
                    && request.PreviewImageId == null),
                "codex-user",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(published));
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            new Mock<IImageRepository>(MockBehavior.Strict),
            publisher: publisher,
            standaloneAttractions: standaloneAttractions);

        ApplicationResult<SocialPublication> result = await service.PublishAsync(
            new SocialLinkPublicationRequest(
                SocialNetwork.Facebook,
                null,
                "https://amusement-parks.fun/fr/attraction/standalone-1/pendolino"),
            "codex-user",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(published, result.Value);
        standaloneAttractions.VerifyAll();
        publisher.VerifyAll();
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
                    request.Message == SocialPublicationMessageBuilder.BuildParkAnnouncementMessage(
                        "Parc Test",
                        "Parc Test",
                        new Uri("https://amusement-parks.fun/fr/park/park-1/park-test"))
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
                "https://amusement-parks.fun/fr/park/park-1/park-test?utm_source=admin",
                "image-current"),
            "codex-user",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(published, result.Value);
        images.VerifyAll();
        publisher.VerifyAll();
    }

    [Fact]
    public async Task PublishAsync_ForParkWithoutCustomTextOrImage_ShouldUseIdempotentAnnouncement()
    {
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<ISocialPublicationService> publisher = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        SocialPublication published = new SocialPublication
        {
            Id = "publication-1",
            Status = SocialPublicationStatus.Published,
            ExternalPostId = "facebook-post-1",
        };
        publisher.Setup(service => service.PublishParkAnnouncementAsync(
                It.Is<Park>(park => park.Id == "park-1"),
                "codex-user",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(published);
        SocialPublicationComposerService service = CreateService(parks, images, publisher: publisher);

        ApplicationResult<SocialPublication> result = await service.PublishAsync(
            new SocialLinkPublicationRequest(
                SocialNetwork.Facebook,
                null,
                "https://amusement-parks.fun/fr/park/park-1/park-test"),
            "codex-user",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(published, result.Value);
        publisher.VerifyAll();
        images.VerifyNoOtherCalls();
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

    [Theory]
    [InlineData("park-operator", ImageOwnerType.ParkOperator, ImageCategory.Operator, "Exploitant Test")]
    [InlineData("park-founder", ImageOwnerType.ParkFounder, ImageCategory.Founder, "Fondatrice Test")]
    [InlineData("park-manufacturer", ImageOwnerType.AttractionManufacturer, ImageCategory.Manufacturer, "Constructeur Test")]
    public async Task ResolveDraftAsync_ForPublicReference_ShouldUseReferenceNameAndImages(
        string routeSegment,
        ImageOwnerType ownerType,
        ImageCategory category,
        string name)
    {
        Mock<IParkOperatorRepository> operators = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        Mock<IParkFounderRepository> founders = new Mock<IParkFounderRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> manufacturers = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        if (routeSegment == "park-operator")
        {
            operators.Setup(repository => repository.GetByIdAsync("reference-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParkOperator
                {
                    Id = "reference-1",
                    Name = name,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                });
        }
        else if (routeSegment == "park-founder")
        {
            founders.Setup(repository => repository.GetByIdAsync("reference-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParkFounder
                {
                    Id = "reference-1",
                    Name = name,
                });
        }
        else
        {
            manufacturers.Setup(repository => repository.GetByIdAsync("reference-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AttractionManufacturer
                {
                    Id = "reference-1",
                    Name = name,
                    IsVisible = true,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                });
        }

        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetPageAsync(
                1,
                6,
                It.Is<ImageSearchCriteria>(criteria =>
                    criteria.OwnerType == ownerType
                    && criteria.OwnerId == "reference-1"
                    && criteria.Category == category
                    && criteria.IsPublished == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(
                new[] { CreateImage("reference-image", "reference-1", true, true, ownerType, category) },
                1,
                6,
                1));
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            images,
            operators: operators,
            founders: founders,
            manufacturers: manufacturers);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            $"https://amusement-parks.fun/fr/{routeSegment}/reference-1/reference-test",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal(SocialPublicationTargetKind.Page, draft.TargetKind);
        Assert.Equal(name, draft.TargetName);
        Assert.Equal("reference-image", Assert.Single(draft.Images.Items).Id);
        images.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForTechnicalDetail_ShouldUseLocalizedTitle()
    {
        Mock<ITechnicalPageRepository> technicalPages = new Mock<ITechnicalPageRepository>(MockBehavior.Strict);
        technicalPages.Setup(repository => repository.GetBySlugAsync("chain-lift", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechnicalPage
            {
                Id = "technical-1",
                Slug = "chain-lift",
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
                Titles = new List<LocalizedText>
                {
                    new LocalizedText("fr", "La chaîne de lift"),
                    new LocalizedText("en", "The chain lift"),
                },
            });
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            new Mock<IImageRepository>(MockBehavior.Strict),
            technicalPages: technicalPages);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/technical/chain-lift",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal("La chaîne de lift", draft.TargetName);
        Assert.Contains("The chain lift", draft.DefaultMessage, StringComparison.Ordinal);
        Assert.Empty(draft.Images.Items);
        technicalPages.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForSharedRankings_ShouldRequirePublicShare()
    {
        DateTime nowUtc = new DateTime(2026, 8, 24, 8, 0, 0, DateTimeKind.Utc);
        Mock<IUserRankingShareRepository> rankingShares = new Mock<IUserRankingShareRepository>(MockBehavior.Strict);
        rankingShares.Setup(repository => repository.GetPublicByShareIdAsync("share-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserRankingShare.Restore(
                "ranking-share-1",
                "user-1",
                true,
                "share-1",
                nowUtc,
                nowUtc,
                nowUtc));
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            new Mock<IImageRepository>(MockBehavior.Strict),
            rankingShares: rankingShares);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/rankings/shared/share-1",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal("Les classements partagés d’un membre", draft.TargetName);
        Assert.Empty(draft.Images.Items);
        rankingShares.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForParkPricing_ShouldUseParkImages()
    {
        Mock<IImageRepository> images = CreateEmptyImagePageRepository(
            ImageOwnerType.Park,
            "park-1",
            ImageCategory.Park);
        SocialPublicationComposerService service = CreateService(CreateParkRepository(), images);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test/pricing",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal("Les tarifs de Parc Test", draft.TargetName);
        images.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForParkZone_ShouldValidateParentAndUseLocalizedName()
    {
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        zones.Setup(repository => repository.GetByIdAsync("zone-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkZone
            {
                Id = "zone-1",
                ParkId = "park-1",
                Name = "Zone Test",
                IsVisible = true,
                Names = new List<LocalizedText>
                {
                    new LocalizedText("fr", "Le Village Test"),
                    new LocalizedText("en", "Test Village"),
                },
            });
        Mock<IImageRepository> images = CreateEmptyImagePageRepository(
            ImageOwnerType.Park,
            "park-1",
            ImageCategory.Park);
        SocialPublicationComposerService service = CreateService(CreateParkRepository(), images, zones: zones);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test/zone/zone-1/test-village",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal("Le Village Test", draft.TargetName);
        Assert.Contains("Test Village", draft.DefaultMessage, StringComparison.Ordinal);
        zones.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForHistoryArticle_ShouldRequirePublishedArticleOwnedByTarget()
    {
        Mock<IHistoryEventRepository> historyEvents = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEvents.Setup(repository => repository.GetByIdAsync("event-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryEvent
            {
                Id = "event-1",
                EntityType = HistoryEntityType.Park,
                OwnerId = "park-1",
                IsVisible = true,
                IsMajor = true,
                Titles = new List<LocalizedText> { new LocalizedText("fr", "Titre de secours") },
                Article = new HistoryArticle
                {
                    IsPublished = true,
                    Titles = new List<LocalizedText>
                    {
                        new LocalizedText("fr", "Une année décisive"),
                        new LocalizedText("en", "A decisive year"),
                    },
                },
            });
        Mock<IImageRepository> images = CreateEmptyImagePageRepository(
            ImageOwnerType.Park,
            "park-1",
            ImageCategory.Park);
        SocialPublicationComposerService service = CreateService(
            CreateParkRepository(),
            images,
            historyEvents: historyEvents);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test/history/event-1/une-annee-decisive",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SocialPublicationDraft draft = Assert.IsType<SocialPublicationDraft>(result.Value);
        Assert.Equal("Une année décisive", draft.TargetName);
        Assert.Contains("A decisive year", draft.DefaultMessage, StringComparison.Ordinal);
        historyEvents.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForClosedParkItemHistory_ShouldRemainShareable()
    {
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Ancienne attraction",
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
                AttractionDetails = new AttractionDetails { Status = "ClosedDefinitively" },
            });
        Mock<IImageRepository> images = CreateEmptyImagePageRepository(
            ImageOwnerType.ParkItem,
            "item-1",
            ImageCategory.ParkItem);
        SocialPublicationComposerService service = CreateService(CreateParkRepository(), images, items: items);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test/item/item-1/ancienne-attraction/history",
            1,
            6,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("L’histoire de Ancienne attraction", result.Value?.TargetName);
        items.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ResolveDraftAsync_ForStandaloneHistoryWithoutPublicTimeline_ShouldReject()
    {
        Mock<IStandaloneAttractionRepository> standaloneAttractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneAttractions.Setup(repository => repository.GetByIdAsync("standalone-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StandaloneAttraction
            {
                Id = "standalone-1",
                Name = "Attraction Test",
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
            });
        Mock<IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>>> historyTimeline =
            new Mock<IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>>>(MockBehavior.Strict);
        historyTimeline.Setup(handler => handler.HandleAsync(
                It.Is<GetStandaloneAttractionHistoryTimelineQuery>(query => query.StandaloneAttractionId == "standalone-1" && query.Page == 1 && !query.IncludeHidden),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<StandaloneAttractionHistoryTimelineResult>.Failure(
                ApplicationErrors.EntityNotFound("History", "standalone-1")));
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            new Mock<IImageRepository>(MockBehavior.Strict),
            standaloneAttractions: standaloneAttractions,
            standaloneHistoryTimeline: historyTimeline);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/attraction/standalone-1/attraction-test/history",
            1,
            6,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "social-publishing.url.invalid");
        standaloneAttractions.VerifyAll();
        historyTimeline.VerifyAll();
    }

    [Theory]
    [InlineData("https://amusement-parks.fun/fr/parkz")]
    [InlineData("https://amusement-parks.fun/fr/park-operator/missing/operator")]
    [InlineData("https://amusement-parks.fun/fr/technical/missing-guide")]
    public async Task ResolveDraftAsync_ForUnknownOrUnvalidatedPublicRoute_ShouldReject(string url)
    {
        Mock<IParkOperatorRepository> operators = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        operators.Setup(repository => repository.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOperator?)null);
        Mock<ITechnicalPageRepository> technicalPages = new Mock<ITechnicalPageRepository>(MockBehavior.Strict);
        technicalPages.Setup(repository => repository.GetBySlugAsync("missing-guide", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechnicalPage?)null);
        SocialPublicationComposerService service = CreateService(
            new Mock<IParkRepository>(MockBehavior.Strict),
            new Mock<IImageRepository>(MockBehavior.Strict),
            operators: operators,
            technicalPages: technicalPages);

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            url,
            1,
            6,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "social-publishing.url.invalid");
    }

    [Fact]
    public async Task ResolveDraftAsync_ForUnknownParkSubpage_ShouldReject()
    {
        SocialPublicationComposerService service = CreateService(
            CreateParkRepository(),
            new Mock<IImageRepository>(MockBehavior.Strict));

        ApplicationResult<SocialPublicationDraft> result = await service.ResolveDraftAsync(
            "https://amusement-parks.fun/fr/park/park-1/park-test/unknown",
            1,
            6,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "social-publishing.url.invalid");
    }

    private static SocialPublicationComposerService CreateService(
        Mock<IParkRepository> parks,
        Mock<IImageRepository> images,
        Mock<IParkItemRepository>? items = null,
        Mock<ISocialPublicationService>? publisher = null,
        Mock<IVideoRepository>? videos = null,
        Mock<IStandaloneAttractionRepository>? standaloneAttractions = null,
        Mock<IParkZoneRepository>? zones = null,
        Mock<IHistoryEventRepository>? historyEvents = null,
        Mock<IParkOperatorRepository>? operators = null,
        Mock<IParkFounderRepository>? founders = null,
        Mock<IAttractionManufacturerRepository>? manufacturers = null,
        Mock<ITechnicalPageRepository>? technicalPages = null,
        Mock<IUserRankingShareRepository>? rankingShares = null,
        Mock<IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>>>? standaloneHistoryTimeline = null)
    {
        Mock<IPublicSeoContextProvider> seoContext = new Mock<IPublicSeoContextProvider>(MockBehavior.Strict);
        seoContext.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicSeoContext("https://amusement-parks.fun", new[] { "de", "en", "es", "fr", "it", "nl", "pl", "pt" }));
        ParkSocialPublicationTargetResolver parkTargetResolver = new ParkSocialPublicationTargetResolver(
            parks.Object,
            items?.Object ?? Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            videos?.Object ?? Mock.Of<IVideoRepository>(MockBehavior.Strict),
            zones?.Object ?? Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            historyEvents?.Object ?? Mock.Of<IHistoryEventRepository>(MockBehavior.Strict));
        StandaloneAttractionSocialPublicationTargetResolver standaloneAttractionTargetResolver =
            new StandaloneAttractionSocialPublicationTargetResolver(
                standaloneAttractions?.Object ?? Mock.Of<IStandaloneAttractionRepository>(MockBehavior.Strict),
                standaloneHistoryTimeline?.Object ?? Mock.Of<IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>>>(MockBehavior.Strict));
        ReferenceSocialPublicationTargetResolver referenceTargetResolver = new ReferenceSocialPublicationTargetResolver(
            operators?.Object ?? Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            founders?.Object ?? Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            manufacturers?.Object ?? Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict));
        ContentSocialPublicationTargetResolver contentTargetResolver = new ContentSocialPublicationTargetResolver(
            technicalPages?.Object ?? Mock.Of<ITechnicalPageRepository>(MockBehavior.Strict),
            rankingShares?.Object ?? Mock.Of<IUserRankingShareRepository>(MockBehavior.Strict));
        SocialPublicationTargetResolver targetResolver = new SocialPublicationTargetResolver(
            seoContext.Object,
            parkTargetResolver,
            standaloneAttractionTargetResolver,
            referenceTargetResolver,
            contentTargetResolver);
        Mock<ISocialPublicationService> effectivePublisher = publisher
            ?? new Mock<ISocialPublicationService>(MockBehavior.Strict);
        if (publisher is null)
        {
            effectivePublisher.Setup(service => service.GetParkAnnouncementAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((SocialPublication?)null);
        }

        return new SocialPublicationComposerService(
            effectivePublisher.Object,
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

    private static Mock<IImageRepository> CreateEmptyImagePageRepository(
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category)
    {
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetPageAsync(
                1,
                6,
                It.Is<ImageSearchCriteria>(criteria =>
                    criteria.OwnerType == ownerType
                    && criteria.OwnerId == ownerId
                    && criteria.Category == category
                    && criteria.IsPublished == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(Array.Empty<Image>(), 1, 6, 0));
        return images;
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
