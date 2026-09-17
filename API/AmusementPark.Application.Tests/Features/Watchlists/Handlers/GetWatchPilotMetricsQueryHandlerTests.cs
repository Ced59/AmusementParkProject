using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Handlers;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists.Handlers;

public sealed class GetWatchPilotMetricsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_AggregatesInteractionsAndEvaluatesTheGate()
    {
        DateTime fromUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
        WatchPilotDailyMetrics day = new(
            "2026-09-16",
            100,
            4,
            new Dictionary<string, long>
            {
                [WatchPilotInteractionKind.NotificationCenterOpened.ToString()] = 12,
                [WatchPilotInteractionKind.SourceOpened.ToString()] = 8,
                [WatchPilotInteractionKind.MisleadingAlertReported.ToString()] = 1,
                [WatchPilotInteractionKind.SubscriptionRemoved.ToString()] = 2,
            });
        WatchPilotMetricsSnapshot snapshot = new(
            5,
            new Dictionary<string, long> { ["OpeningDateConfirmed"] = 5 },
            3,
            2,
            1,
            0,
            100,
            0,
            120m,
            4,
            0,
            98,
            2,
            0,
            true,
            0,
            0,
            0,
            new Dictionary<string, long>(),
            [day]);
        Mock<IWatchPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.ReadAsync(fromUtc, toUtc, CancellationToken.None))
            .ReturnsAsync(snapshot);
        GetWatchPilotMetricsQueryHandler handler = new(
            repository.Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));

        ApplicationResult<WatchPilotMetricsResult> result = await handler.HandleAsync(
            new GetWatchPilotMetricsQuery(fromUtc, toUtc),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value.ActiveSubscriptions);
        Assert.Equal(12, result.Value.NotificationCenterOpens);
        Assert.Equal(8, result.Value.SourceOpens);
        Assert.Equal(2, result.Value.SubscriptionsRemoved);
        Assert.Equal(WatchPilotSignal.ReadyToExtend, result.Value.Health.Signal);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_RejectsAnInvertedRange()
    {
        Mock<IWatchPilotMetricsRepository> repository = new(MockBehavior.Strict);
        GetWatchPilotMetricsQueryHandler handler = new(repository.Object);

        ApplicationResult<WatchPilotMetricsResult> result = await handler.HandleAsync(
            new GetWatchPilotMetricsQuery(
                new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.VerifyNoOtherCalls();
    }
}
