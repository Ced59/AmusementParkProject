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

public sealed class LiveWaitForecastBacktestReaderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReadAsync_ShouldResolveHiddenMappedTargetAndFilterFormerMapping()
    {
        DateTime evaluationFromUtc = NowUtc.AddDays(-90);
        DateTime historyFromUtc = evaluationFromUtc.AddDays(-84);
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        history.Setup(repository => repository.GetAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                LiveTargetType.ParkItem,
                "item-1",
                "usage-1",
                It.IsAny<string>(),
                TimeSpan.FromHours(1),
                historyFromUtc,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                CreateObservation(NowUtc.AddDays(-1), "former-item"),
                CreateObservation(NowUtc.AddHours(-1), "external-item-1"),
            });
        LiveWaitForecastBacktestReader reader = CreateReader(history);

        ApplicationResult<LiveWaitForecastBacktestResult> result = await reader.ReadAsync(
            "item-1",
            null,
            null,
            CancellationToken.None);

        LiveWaitForecastBacktestResult value = Assert.IsType<LiveWaitForecastBacktestResult>(
            result.Value);
        Assert.Equal("Attraction", value.TargetDisplayName);
        Assert.Equal("Park", value.ParkDisplayName);
        Assert.Equal(LiveWaitForecastBacktestVerdict.InsufficientData, value.Report.Verdict);
        Assert.Equal(1, value.Report.SourceObservationCount);
        history.VerifyAll();
    }

    [Fact]
    public async Task ReadAsync_ShouldRejectPeriodBeforeLoadingEntities()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        LiveWaitForecastBacktestReader reader = CreateReader(history, parks, items);

        ApplicationResult<LiveWaitForecastBacktestResult> result = await reader.ReadAsync(
            "item-1",
            new DateTimeOffset(NowUtc.AddDays(-5)),
            new DateTimeOffset(NowUtc),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.backtest.period.invalid", Assert.Single(result.Errors).Code);
        parks.VerifyNoOtherCalls();
        items.VerifyNoOtherCalls();
        history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadAsync_ShouldFailWhenTargetHasNoEligibleMapping()
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
        LiveWaitForecastBacktestReader reader = CreateReader(history, mappings: mappings);

        ApplicationResult<LiveWaitForecastBacktestResult> result = await reader.ReadAsync(
            "item-1",
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.backtest.unavailable", Assert.Single(result.Errors).Code);
        history.VerifyNoOtherCalls();
    }

    private static LiveWaitForecastBacktestReader CreateReader(
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
                    true,
                    CancellationToken.None))
                .ReturnsAsync(new Park { Id = "park-1", Name = "Park", IsVisible = false });
        }

        if (items is null)
        {
            itemRepository.Setup(repository => repository.GetByIdAsync(
                    "item-1",
                    true,
                    CancellationToken.None))
                .ReturnsAsync(new ParkItem
                {
                    Id = "item-1",
                    ParkId = "park-1",
                    Name = "Attraction",
                    IsVisible = false,
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
                .ReturnsAsync(new[]
                {
                    new LivePublicTargetCoverage("item-1", "external-item-1"),
                });
        }

        Mock<ILiveDataSourceCatalog> catalog = new Mock<ILiveDataSourceCatalog>(MockBehavior.Strict);
        LivePollingTarget pollingTarget = CreatePollingTarget();
        catalog.SetupGet(value => value.ConfiguredPollingTarget).Returns(pollingTarget);
        catalog.Setup(value => value.Find(LiveDataSourceId.Parse("themeparks-wiki")))
            .Returns(CreatePresentation());
        Mock<TimeProvider> clock = new Mock<TimeProvider>(MockBehavior.Strict);
        clock.Setup(value => value.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        LiveWaitForecastBacktestPolicy policy = new LiveWaitForecastBacktestPolicy();
        return new LiveWaitForecastBacktestReader(
            parkRepository.Object,
            itemRepository.Object,
            history.Object,
            mappingRepository.Object,
            catalog.Object,
            new LiveWaitForecastBacktestCalculator(policy),
            policy,
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
        string externalTargetId)
    {
        return new LiveWaitHistoryObservation(
            externalTargetId,
            "mapping-1",
            observedAtUtc,
            observedAtUtc.AddSeconds(1),
            LiveOperationalStatus.Open,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 20, false) },
            false);
    }
}
