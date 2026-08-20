using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class StandaloneAttractionHistorySitemapSectionProviderTests
{
    [Theory]
    [InlineData("ClosedDefinitively")]
    [InlineData("permanently-closed")]
    public void IsPublicHistoryStandaloneAttraction_WhenAttractionIsPermanentlyClosed_ShouldReturnFalse(string status)
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Pendolino",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails
            {
                Status = status,
            },
        };

        bool isPublic = HistorySitemapCandidateResolver.IsPublicHistoryStandaloneAttraction(attraction);

        Assert.False(isPublic);
    }

    [Fact]
    public async Task GetUrlsAsync_WhenPublicStandaloneAttractionHasOpeningYear_ShouldReturnTimelineUrl()
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Pendolino",
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            UpdatedAtUtc = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            AttractionDetails = new AttractionDetails
            {
                OpeningDateText = "2007",
            },
        };

        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> standaloneAttractionRepository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);

        historyRepository
            .Setup(repository => repository.GetPublicVisibleEventsAsync(50000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());
        SetupEmptyPublicHistoryParks(parkRepository);
        SetupEmptyPublicHistoryItems(itemRepository);
        standaloneAttractionRepository
            .Setup(repository => repository.GetPublicSitemapCandidatesAsync(50000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attraction });
        itemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        standaloneAttractionRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "standalone-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attraction });
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => !ids.Any()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Park>());

        HistoryTimelinesSitemapSectionProvider provider = new HistoryTimelinesSitemapSectionProvider(
            historyRepository.Object,
            parkRepository.Object,
            itemRepository.Object,
            standaloneAttractionRepository.Object);

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(
            new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } },
            CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url =>
            url.RelativePath == "/fr/attraction/standalone-1/pendolino/history" &&
            url.ChangeFrequency == "monthly" &&
            url.Priority == 0.70m);
        Assert.Contains(urls, static url => url.RelativePath == "/en/attraction/standalone-1/pendolino/history");
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        standaloneAttractionRepository.VerifyAll();
    }

    private static void SetupEmptyPublicHistoryParks(Mock<IParkRepository> repository)
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
                ClosedEntityFilter.All,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                false,
                null))
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
                bool sortDescending,
                ParkAudienceClassificationFilter? audienceClassificationFilter) =>
                Task.FromResult(new PagedResult<Park>(Array.Empty<Park>(), page, pageSize, 0)));
    }

    private static void SetupEmptyPublicHistoryItems(Mock<IParkItemRepository> repository)
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
                Task.FromResult(new PagedResult<ParkItem>(Array.Empty<ParkItem>(), page, pageSize, 0)));
    }
}
