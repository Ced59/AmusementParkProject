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

public sealed class PublicLiveForecastReaderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReadParkItemAsync_ShouldPublishOnlyAnEligibleForecastWithEvidence()
    {
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        DateTime historyFromUtc = NowUtc.AddDays(-174);
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
            .ReturnsAsync(CreateEligibleObservations());
        PublicLiveForecastReader reader = CreateReader(history);

        ApplicationResult<PublicLiveForecastResult> result = await reader.ReadParkItemAsync(
            "item-1",
            CancellationToken.None);

        PublicLiveForecastResult forecast = Assert.IsType<PublicLiveForecastResult>(result.Value);
        Assert.Equal("Attraction", forecast.TargetDisplayName);
        Assert.Equal("Park", forecast.ParkDisplayName);
        Assert.Equal(LiveWaitForecastBacktestPolicy.CandidateMethod, forecast.Method);
        Assert.Equal(LiveWaitForecastBacktestPolicy.IntervalMethod, forecast.IntervalMethod);
        Assert.Equal(0d, forecast.MeanAbsoluteErrorMinutes);
        Assert.Equal(100d, forecast.IntervalCoveragePercent);
        Assert.True(forecast.EvaluationPointCount >= 100);
        Assert.Equal(30d, forecast.Forecast.ExpectedWaitMinutes);
        Assert.Equal(NowUtc.AddMinutes(28), forecast.FreshnessExpiresAtUtc);
        Assert.Equal("Powered by ThemeParks.wiki", forecast.Source.AttributionText);
        history.VerifyAll();
    }

    [Fact]
    public async Task ReadParkItemAsync_ShouldHideForecastWhenBacktestRejectsCandidate()
    {
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        history.Setup(repository => repository.GetAsync(
                It.IsAny<LiveDataSourceId>(),
                LiveTargetType.ParkItem,
                "item-1",
                "usage-1",
                It.IsAny<string>(),
                TimeSpan.FromHours(1),
                It.IsAny<DateTime>(),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(CreateConstantObservations());
        PublicLiveForecastReader reader = CreateReader(history);

        ApplicationResult<PublicLiveForecastResult> result = await reader.ReadParkItemAsync(
            "item-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.forecast.unavailable", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task ReadParkItemAsync_ShouldStopBeforeHistoryWhenPublicGateIsClosed()
    {
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-park-1",
                CancellationToken.None))
            .ReturnsAsync(new LiveOperationalGateSnapshot(
                true,
                false,
                "external-park-1",
                Array.Empty<LiveOperationalControl>(),
                new LiveOperationalControlPolicy()));
        PublicLiveForecastReader reader = CreateReader(history, gate);

        ApplicationResult<PublicLiveForecastResult> result = await reader.ReadParkItemAsync(
            "item-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.forecast.unavailable", Assert.Single(result.Errors).Code);
        history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadParkItemAsync_ShouldHideForecastWhenLatestStateIsClosed()
    {
        Mock<ILiveHistoryStatisticsRepository> history =
            new Mock<ILiveHistoryStatisticsRepository>(MockBehavior.Strict);
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest.Setup(repository => repository.GetByTargetAsync(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new[] { CreateLatestObservation(LiveOperationalStatus.Closed) });
        PublicLiveForecastReader reader = CreateReader(history, configuredLatest: latest);

        ApplicationResult<PublicLiveForecastResult> result = await reader.ReadParkItemAsync(
            "item-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.forecast.unavailable", Assert.Single(result.Errors).Code);
        history.VerifyNoOtherCalls();
    }

    private static PublicLiveForecastReader CreateReader(
        Mock<ILiveHistoryStatisticsRepository> history,
        Mock<ILiveOperationalGate>? configuredGate = null,
        Mock<ILiveLatestObservationRepository>? configuredLatest = null)
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                "park-1",
                false,
                CancellationToken.None))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park", IsVisible = true });
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetByIdAsync(
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
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        mappings.Setup(repository => repository.GetEligiblePublicTargetCoverageByParkAsync(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-park-1",
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new[] { new LivePublicTargetCoverage("item-1", "external-item-1") });
        Mock<ILiveDataSourceCatalog> catalog = new Mock<ILiveDataSourceCatalog>(MockBehavior.Strict);
        LivePollingTarget pollingTarget = CreatePollingTarget();
        catalog.SetupGet(value => value.IsPublicReadEnabled).Returns(true);
        catalog.SetupGet(value => value.PublicPollingTarget).Returns(pollingTarget);
        catalog.Setup(value => value.Find(LiveDataSourceId.Parse("themeparks-wiki")))
            .Returns(CreatePresentation());
        Mock<ILiveOperationalGate> gate = configuredGate
            ?? new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        if (configuredGate is null)
        {
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
        }

        Mock<ILiveLatestObservationRepository> latest = configuredLatest
            ?? new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        if (configuredGate is null && configuredLatest is null)
        {
            latest.Setup(repository => repository.GetByTargetAsync(
                    LiveTargetType.ParkItem,
                    "item-1",
                    "park-1",
                    CancellationToken.None))
                .ReturnsAsync(new[] { CreateLatestObservation() });
        }

        Mock<TimeProvider> clock = new Mock<TimeProvider>(MockBehavior.Strict);
        clock.Setup(value => value.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        LiveWaitForecastBacktestPolicy policy = new LiveWaitForecastBacktestPolicy();
        return new PublicLiveForecastReader(
            parks.Object,
            items.Object,
            history.Object,
            latest.Object,
            mappings.Object,
            catalog.Object,
            gate.Object,
            new LiveWaitForecastBacktestCalculator(policy),
            new LiveWaitForecastCalculator(policy),
            policy,
            new LiveLatestObservationSelectionPolicy(),
            clock.Object);
    }

    private static IReadOnlyCollection<LiveWaitHistoryObservation> CreateEligibleObservations()
    {
        return Enumerable.Range(1, 174)
            .SelectMany(day => new[]
            {
                NowUtc.Date.AddDays(-day).AddHours(13),
                NowUtc.Date.AddDays(-day).AddHours(14),
            })
            .Select(timestamp => CreateObservation(
                timestamp,
                10 + (((int)timestamp.DayOfWeek) * 10)))
            .ToArray();
    }

    private static IReadOnlyCollection<LiveWaitHistoryObservation> CreateConstantObservations()
    {
        return Enumerable.Range(1, 174)
            .SelectMany(day => new[]
            {
                NowUtc.Date.AddDays(-day).AddHours(13),
                NowUtc.Date.AddDays(-day).AddHours(14),
            })
            .Select(timestamp => CreateObservation(timestamp, 20))
            .ToArray();
    }

    private static LiveWaitHistoryObservation CreateObservation(DateTime timestamp, int waitMinutes)
    {
        return new LiveWaitHistoryObservation(
            "external-item-1",
            "mapping-1",
            timestamp,
            timestamp.AddSeconds(1),
            LiveOperationalStatus.Open,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, waitMinutes, false) },
            false);
    }

    private static LiveLatestObservation CreateLatestObservation(
        LiveOperationalStatus status = LiveOperationalStatus.Open)
    {
        DateTime observedAtUtc = NowUtc.AddMinutes(-2);
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            status,
            status == LiveOperationalStatus.Closed
                ? Array.Empty<LiveQueueObservation>()
                : new[] { new LiveQueueObservation(LiveQueueKind.Standby, 20, false) },
            new LiveObservationProvenance(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-item-1",
                observedAtUtc,
                observedAtUtc.AddSeconds(1),
                observedAtUtc.AddSeconds(2),
                "correlation",
                "adapter-1",
                "mapping-1",
                LiveDataConfidence.Medium,
                "usage-1",
                "transformation-1"),
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(1)),
            null);
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
}
