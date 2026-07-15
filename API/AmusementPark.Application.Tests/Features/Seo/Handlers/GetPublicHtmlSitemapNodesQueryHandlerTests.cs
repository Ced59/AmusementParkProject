using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Handlers;

public sealed class GetPublicHtmlSitemapNodesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParentIsRoot_ShouldReturnOnlyPublicCurrentLanguageNodes()
    {
        GetPublicHtmlSitemapNodesQueryHandler handler = CreateHandler();

        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> result = await handler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery("fr", null, new[] { "fr", "en" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, static node => node.Id == "sitemap" && node.RelativeUrl == "/fr/sitemap");
        Assert.Contains(result.Value, static node => node.Id == "parks" && node.RelativeUrl == "/fr/parks" && node.HasChildren);
        Assert.Contains(result.Value, static node => node.Id == "technical" && node.RelativeUrl == "/fr/technical" && node.HasChildren);
        Assert.DoesNotContain(result.Value, static node => node.Id.Contains("admin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Value, static node => node.RelativeUrl?.Contains("/login", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(result.Value, static node => node.RelativeUrl?.Contains("/profile", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageIsNotSupported_ShouldReturnValidationError()
    {
        GetPublicHtmlSitemapNodesQueryHandler handler = CreateHandler();

        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> result = await handler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery("jp", null, new[] { "fr", "en" }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "seo.html-sitemap.language.invalid");
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeDescendants_ShouldReadLinksFromSitemapSnapshot()
    {
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            Sections = new[]
            {
                new SitemapSectionStats("parks-fr", "parks-fr.xml", "Parcs · FR", 2, null),
                new SitemapSectionStats("parks-en", "parks-en.xml", "Parcs · EN", 1, null),
                new SitemapSectionStats("static-fr", "static-fr.xml", "Pages statiques · FR", 1, null),
            },
        };

        Mock<ISeoSitemapSnapshotRepository> snapshotRepository = new Mock<ISeoSitemapSnapshotRepository>(MockBehavior.Strict);
        snapshotRepository
            .Setup(repository => repository.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        snapshotRepository
            .Setup(repository => repository.GetSectionXmlAsync("parks-fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
                  <url><loc>https://amusement-parks.fun/fr/park/park-1/magic-park</loc></url>
                  <url><loc>https://amusement-parks.fun/en/park/park-1/magic-park</loc></url>
                </urlset>
                """);
        snapshotRepository
            .Setup(repository => repository.GetSectionXmlAsync("static-fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
                  <url><loc>https://amusement-parks.fun/fr/sitemap</loc></url>
                </urlset>
                """);

        GetPublicHtmlSitemapNodesQueryHandler handler = CreateHandler(snapshotRepository);

        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> result = await handler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery("fr", null, new[] { "fr", "en" }, IncludeDescendants: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        PublicHtmlSitemapNode parksNode = Assert.Single(result.Value, static node => node.Id == "sitemap-section:parks-fr");
        IReadOnlyCollection<PublicHtmlSitemapNode> parkChildren = parksNode.Children ?? throw new InvalidOperationException();
        PublicHtmlSitemapNode parkNode = Assert.Single(parkChildren);
        Assert.Equal("/fr/park/park-1/magic-park", parkNode.RelativeUrl);
        Assert.DoesNotContain(parkChildren, static node => node.RelativeUrl?.StartsWith("/en/", StringComparison.OrdinalIgnoreCase) == true);
        PublicHtmlSitemapNode staticNode = Assert.Single(result.Value, static node => node.Id == "sitemap-section:static-fr");
        Assert.Contains(staticNode.Children ?? Array.Empty<PublicHtmlSitemapNode>(), static node => node.RelativeUrl == "/fr/sitemap");
        snapshotRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenClosedVisibleItemHasHistory_ShouldExposeParkHistoryNode()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Magic Park",
            IsVisible = true,
        };
        ParkItem closedItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Old Ride",
            IsVisible = true,
            AttractionDetails = new AttractionDetails { Status = ParkItemStatusNormalizer.ClosedDefinitively },
        };
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "history-1",
            Key = "old-ride-closure",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = "item-1",
            Year = 2004,
            EventType = ParkItemHistoryEventType.DefinitiveClosure.ToString(),
            IsVisible = true,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        itemRepository
            .Setup(repository => repository.GetPublicPageByParkIdAsync(
                1,
                It.IsAny<int>(),
                "park-1",
                null,
                false,
                ClosedEntityFilter.OpenOnly,
                null,
                null,
                null,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ParkItem>(Array.Empty<ParkItem>(), 1, 500, 0));
        itemRepository
            .Setup(repository => repository.GetPublicPageByParkIdAsync(
                1,
                It.IsAny<int>(),
                "park-1",
                null,
                false,
                ClosedEntityFilter.All,
                null,
                null,
                null,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ParkItem>(new[] { closedItem }, 1, 500, 1));

        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(repository => repository.GetSummariesByParkIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>(StringComparer.OrdinalIgnoreCase));

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetPageAsync(1, 1, It.IsAny<ImageSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(Array.Empty<Image>(), 1, 1, 0));

        Mock<IVideoRepository> videoRepository = new Mock<IVideoRepository>(MockBehavior.Strict);
        videoRepository
            .Setup(repository => repository.GetPageAsync(1, It.IsAny<int>(), It.IsAny<VideoSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Video>(Array.Empty<Video>(), 1, 1, 0));

        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(repository => repository.GetParkTimelineSummaryAsync(
                "park-1",
                false,
                true,
                It.Is<IReadOnlyCollection<string>>(itemIds => itemIds.Contains("item-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { historyEvent });
        GetPublicHtmlSitemapNodesQueryHandler handler = CreateHandler(
            parkRepository,
            itemRepository,
            openingHoursRepository,
            imageRepository,
            videoRepository,
            historyRepository);

        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> result = await handler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery("fr", "park:park-1", new[] { "fr", "en" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, static node => node.Id == "park-items:park-1" && node.RelativeUrl == "/fr/park/park-1/magic-park/items");
        Assert.Contains(result.Value, static node => node.Id == "park-history:park-1" && node.RelativeUrl == "/fr/park/park-1/magic-park/history");

        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> itemNodesResult = await handler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery("fr", "park-items:park-1", new[] { "fr", "en" }),
            CancellationToken.None);

        Assert.True(itemNodesResult.IsSuccess);
        Assert.NotNull(itemNodesResult.Value);
        Assert.Contains(itemNodesResult.Value, static node => node.Id == "park-item:item-1" && node.RelativeUrl == "/fr/park/park-1/magic-park/item/item-1/old-ride/history");
        itemRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    private static GetPublicHtmlSitemapNodesQueryHandler CreateHandler(Mock<ISeoSitemapSnapshotRepository>? sitemapSnapshotRepository = null)
    {
        return CreateHandler(
            new Mock<IParkRepository>(),
            new Mock<IParkItemRepository>(),
            new Mock<IParkOpeningHoursRepository>(),
            new Mock<IImageRepository>(),
            new Mock<IVideoRepository>(),
            new Mock<IHistoryEventRepository>(),
            sitemapSnapshotRepository: sitemapSnapshotRepository);
    }

    private static GetPublicHtmlSitemapNodesQueryHandler CreateHandler(
        Mock<IParkRepository> parkRepository,
        Mock<IParkItemRepository> parkItemRepository,
        Mock<IParkOpeningHoursRepository> openingHoursRepository,
        Mock<IImageRepository> imageRepository,
        Mock<IVideoRepository> videoRepository,
        Mock<IHistoryEventRepository> historyEventRepository,
        Mock<IParkZoneRepository>? parkZoneRepository = null,
        Mock<IParkOperatorRepository>? parkOperatorRepository = null,
        Mock<IParkFounderRepository>? parkFounderRepository = null,
        Mock<IAttractionManufacturerRepository>? attractionManufacturerRepository = null,
        Mock<ITechnicalPageRepository>? technicalPageRepository = null,
        Mock<ISeoSitemapSnapshotRepository>? sitemapSnapshotRepository = null)
    {
        return new GetPublicHtmlSitemapNodesQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            parkZoneRepository?.Object ?? Mock.Of<IParkZoneRepository>(),
            openingHoursRepository.Object,
            imageRepository.Object,
            videoRepository.Object,
            historyEventRepository.Object,
            parkOperatorRepository?.Object ?? Mock.Of<IParkOperatorRepository>(),
            parkFounderRepository?.Object ?? Mock.Of<IParkFounderRepository>(),
            attractionManufacturerRepository?.Object ?? Mock.Of<IAttractionManufacturerRepository>(),
            technicalPageRepository?.Object ?? Mock.Of<ITechnicalPageRepository>(),
            sitemapSnapshotRepository?.Object ?? Mock.Of<ISeoSitemapSnapshotRepository>());
    }
}
