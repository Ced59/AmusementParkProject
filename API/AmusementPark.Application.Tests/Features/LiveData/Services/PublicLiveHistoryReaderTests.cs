using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class PublicLiveHistoryReaderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReadParkItemAsync_ReturnsNamedTargetAndExplicitCoverage()
    {
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        DateTime fromUtc = NowUtc.AddDays(-1);
        history.Setup(repository => repository.GetAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                LiveTargetType.ParkItem,
                "item-1",
                "usage-1",
                It.IsAny<string>(),
                TimeSpan.FromHours(1),
                fromUtc,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                CreateObservation(new DateTime(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc), 20),
            });
        PublicLiveHistoryReader reader = CreateReader(history);

        ApplicationResult<PublicLiveHistoryResult> result = await reader.ReadParkItemAsync(
            "item-1",
            new DateTimeOffset(fromUtc),
            new DateTimeOffset(NowUtc),
            "hour",
            CancellationToken.None);

        PublicLiveHistoryResult value = Assert.IsType<PublicLiveHistoryResult>(result.Value);
        Assert.Equal("Attraction", value.DisplayName);
        Assert.Equal("Park", value.ParkDisplayName);
        Assert.Equal(LiveWaitHistoryDataStatus.Insufficient, value.DataStatus);
        Assert.Equal(1, value.ObservationCount);
        Assert.Equal(1, value.UsableWaitCount);
        Assert.Equal("Powered by ThemeParks.wiki", value.Source.AttributionText);
        Assert.DoesNotContain(value.Hours, hour => hour.LocalHour is < 8 or >= 20);
    }

    [Fact]
    public async Task ReadParkItemAsync_RejectsUnsupportedBucketBeforeReadingEntities()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        PublicLiveHistoryReader reader = CreateReader(history, parks, items);

        ApplicationResult<PublicLiveHistoryResult> result = await reader.ReadParkItemAsync(
            "item-1",
            null,
            null,
            "minute",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.history.period.invalid", Assert.Single(result.Errors).Code);
        parks.VerifyNoOtherCalls();
        items.VerifyNoOtherCalls();
        history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadParkItemAsync_HidesHistoryWhenTargetIsNotCovered()
    {
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        mappings.Setup(repository => repository.GetEligiblePublicTargetCoverageByParkAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-park-1",
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<LivePublicTargetCoverage>());
        PublicLiveHistoryReader reader = CreateReader(history, mappings: mappings);

        ApplicationResult<PublicLiveHistoryResult> result = await reader.ReadParkItemAsync(
            "item-1",
            null,
            null,
            "hour",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.public-read.disabled", Assert.Single(result.Errors).Code);
        history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadParkItemAsync_ExcludesObservationsFromAFormerTargetMapping()
    {
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        DateTime fromUtc = NowUtc.AddDays(-1);
        history.Setup(repository => repository.GetAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                LiveTargetType.ParkItem,
                "item-1",
                "usage-1",
                It.IsAny<string>(),
                TimeSpan.FromHours(1),
                fromUtc,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                CreateObservation(
                    new DateTime(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc),
                    20,
                    "former-external-item"),
            });
        PublicLiveHistoryReader reader = CreateReader(history);

        ApplicationResult<PublicLiveHistoryResult> result = await reader.ReadParkItemAsync(
            "item-1",
            new DateTimeOffset(fromUtc),
            new DateTimeOffset(NowUtc),
            "hour",
            CancellationToken.None);

        PublicLiveHistoryResult value = Assert.IsType<PublicLiveHistoryResult>(result.Value);
        Assert.Equal(LiveWaitHistoryDataStatus.Unavailable, value.DataStatus);
        Assert.Equal(0, value.ObservationCount);
        Assert.Equal(0, value.UsableWaitCount);
    }

    private static PublicLiveHistoryReader CreateReader(
        Mock<ILiveHistoryStatisticsRepository> history,
        Mock<IParkRepository>? parks = null,
        Mock<IParkItemRepository>? items = null,
        Mock<ILiveTargetMappingRepository>? mappings = null)
    {
        Mock<IParkRepository> parkRepository = parks
            ?? new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = items
            ?? new Mock<IParkItemRepository>(MockBehavior.Strict);
        if (parks is null)
        {
            parkRepository.Setup(repository => repository.GetByIdAsync(
                    "park-1",
                    false,
                    CancellationToken.None))
                .ReturnsAsync(new Park { Id = "park-1", Name = "Park", IsVisible = true });
        }

        if (items is null)
        {
            itemRepository.Setup(repository => repository.GetByIdAsync(
                    "item-1",
                    false,
                    CancellationToken.None))
                .ReturnsAsync(new ParkItem
                {
                    Id = "item-1",
                    ParkId = "park-1",
                    Name = "Attraction",
                    IsVisible = true,
                });
        }

        Mock<ILiveTargetMappingRepository> mappingRepository = mappings
            ?? new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        if (mappings is null)
        {
            mappingRepository.Setup(repository => repository.GetEligiblePublicTargetCoverageByParkAsync(
                    LiveDataSourceId.Parse("themeparks-wiki"),
                    "external-park-1",
                    "park-1",
                    CancellationToken.None))
                .ReturnsAsync(new[] { new LivePublicTargetCoverage("item-1", "external-item-1") });
        }

        Mock<ILiveDataSourceCatalog> catalog = new Mock<ILiveDataSourceCatalog>(MockBehavior.Strict);
        LivePollingTarget pollingTarget = CreatePollingTarget();
        catalog.SetupGet(value => value.IsPublicReadEnabled).Returns(true);
        catalog.SetupGet(value => value.PublicPollingTarget).Returns(pollingTarget);
        catalog.Setup(value => value.Find(LiveDataSourceId.Parse("themeparks-wiki")))
            .Returns(CreatePresentation());
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-park-1",
                CancellationToken.None))
            .ReturnsAsync(new LiveOperationalGateSnapshot(
                true,
                true,
                "external-park-1",
                Array.Empty<LiveOperationalControl>(),
                new LiveOperationalControlPolicy()));
        Mock<TimeProvider> clock = new Mock<TimeProvider>(MockBehavior.Strict);
        clock.Setup(value => value.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        return new PublicLiveHistoryReader(
            parkRepository.Object,
            itemRepository.Object,
            history.Object,
            mappingRepository.Object,
            catalog.Object,
            gate.Object,
            new LiveWaitHistoryStatisticsCalculator(),
            clock.Object);
    }

    private static LiveDataSourcePresentation CreatePresentation()
    {
        return new LiveDataSourcePresentation(
            new LiveDataSource(
                LiveDataSourceId.Parse("themeparks-wiki"),
                LiveDataSourceType.AuthorizedAggregator,
                "ThemeParks.wiki",
                new SourceUsagePolicy(
                    "usage-1",
                    "https://themeparks.wiki/terms",
                    true,
                    true,
                    false,
                    true,
                    "attribution",
                    NowUtc),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30),
                LiveDataSourceStatus.Active,
                new LiveHistoryRetentionPolicy(
                    TimeSpan.FromDays(7),
                    TimeSpan.FromDays(400),
                    TimeSpan.FromHours(1))),
            100,
            "Powered by ThemeParks.wiki",
            "https://themeparks.wiki/");
    }

    private static LivePollingTarget CreatePollingTarget()
    {
        return new LivePollingTarget(
            LiveDataSourceId.Parse("themeparks-wiki"),
            "external-park-1",
            new LivePollingActiveWindow(TimeZoneInfo.Utc, 8, 20),
            new LivePollingPolicy(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromHours(1),
                5,
                TimeSpan.FromMinutes(30)),
            TimeSpan.Zero);
    }

    private static LiveWaitHistoryObservation CreateObservation(
        DateTime observedAtUtc,
        int waitMinutes,
        string externalTargetId = "external-item-1")
    {
        return new LiveWaitHistoryObservation(
            externalTargetId,
            "mapping-1",
            observedAtUtc,
            observedAtUtc.AddSeconds(1),
            LiveOperationalStatus.Open,
            new[]
            {
                new LiveQueueObservation(LiveQueueKind.Standby, waitMinutes, false),
            },
            false);
    }
}
