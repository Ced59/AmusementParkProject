using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class HistoryKeyYearSitemapSectionProviderTests
{
    [Fact]
    public async Task GetUrlsAsync_WithDocumentedKeyYear_AddsTimelineAndYearOnly()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        park.Name = "Parc témoin";
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("item-1", "Attraction témoin");
        HistoricalSubject parkSubject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            park.Id,
            park.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalSubject itemSubject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            item.Id,
            item.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalFact parkFact = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 1988);
        HistoricalFact itemFact = PublicParkHistoryTestData.CreateOpeningFact(itemSubject, 1988);
        HistoricalFact[] facts = { parkFact, itemFact };

        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalKeyYearFactReader> factReader = new Mock<IHistoricalKeyYearFactReader>(MockBehavior.Strict);
        Mock<IParkHistoricalSnapshotBuilder> snapshotBuilder = new Mock<IParkHistoricalSnapshotBuilder>(MockBehavior.Strict);

        historyRepository
            .Setup(repository => repository.GetPublicVisibleEventsAsync(50000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());
        SetupPublicParks(parkRepository, new[] { park });
        SetupPublicItems(itemRepository, new[] { item });
        zoneRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factReader
            .Setup(reader => reader.GetLatestDecisionEligibleRevisionsAsync(50000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(facts);
        snapshotBuilder
            .Setup(builder => builder.Build(
                park.Id,
                It.Is<HistoricalInstant>(instant => instant.Year == 1988),
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>()))
            .Returns(CreateEligibleSnapshot(park.Id, parkSubject, parkFact.Id, itemFact.Id));

        HistoryTimelinesSitemapSectionProvider provider = new HistoryTimelinesSitemapSectionProvider(
            historyRepository.Object,
            parkRepository.Object,
            itemRepository.Object,
            null,
            zoneRepository.Object,
            factReader.Object,
            new HistoricalParkRolloutGateAssessmentService(
                snapshotBuilder.Object,
                new HistoricalParkRolloutGateEvaluator()));

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(
            new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } },
            CancellationToken.None);

        Assert.Equal(4, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-temoin/history");
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/parc-temoin/history");
        Assert.Contains(urls, static url =>
            url.RelativePath == "/fr/park/park-1/parc-temoin/history/1988"
            && url.ChangeFrequency == "yearly"
            && url.Priority == 0.68m);
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/parc-temoin/history/1988");
        Assert.DoesNotContain(urls, static url => url.RelativePath.EndsWith("/1989", StringComparison.Ordinal));

        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        zoneRepository.VerifyAll();
        factReader.VerifyAll();
        snapshotBuilder.VerifyAll();
    }

    private static ParkHistoricalSnapshot CreateEligibleSnapshot(
        string parkId,
        HistoricalSubject subject,
        params Guid[] supportingFactIds)
    {
        HistoricalSubjectSnapshot subjectSnapshot = new HistoricalSubjectSnapshot(
            subject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            Array.Empty<HistoricalAttributeSnapshot>(),
            Array.Empty<HistoricalSnapshotReason>(),
            supportingFactIds);
        HistoricalCoverage coverage = new HistoricalCoverage(
            1,
            1,
            0,
            0,
            new HistoricalFieldCoverage(1, 1),
            new HistoricalFieldCoverage(0, 0),
            new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc),
            HistoricalCoverageStatus.Substantial);

        return new ParkHistoricalSnapshot(
            parkId,
            HistoricalInstant.ForYear(1988),
            new[] { subjectSnapshot },
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
    }

    private static void SetupPublicParks(
        Mock<IParkRepository> repository,
        IReadOnlyCollection<Park> parks)
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
            {
                IReadOnlyCollection<Park> pageItems = parks
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                return Task.FromResult(new PagedResult<Park>(pageItems, page, pageSize, parks.Count));
            });
    }

    private static void SetupPublicItems(
        Mock<IParkItemRepository> repository,
        IReadOnlyCollection<ParkItem> items)
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
}
