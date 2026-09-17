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
        DateTime requestedToUtc = new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new DateTime(
            2026, 9, 17, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999);
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
            new GetWatchPilotMetricsQuery(fromUtc, requestedToUtc),
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
    public async Task HandleAsync_NormalizesAllMetricsToTheRequestedUtcCalendarDays()
    {
        DateTime expectedFromUtc = new(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);
        DateTime expectedToUtc = new DateTime(
            2026, 9, 17, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999);
        Mock<IWatchPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.ReadAsync(
                expectedFromUtc,
                expectedToUtc,
                CancellationToken.None))
            .ReturnsAsync(EmptySnapshot());
        GetWatchPilotMetricsQueryHandler handler = new(repository.Object);

        ApplicationResult<WatchPilotMetricsResult> result = await handler.HandleAsync(
            new GetWatchPilotMetricsQuery(
                new DateTime(2026, 9, 16, 12, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 17, 13, 45, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedFromUtc, result.Value.FromUtc);
        Assert.Equal(expectedToUtc, result.Value.ToUtc);
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

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(9999, 12, 31)]
    public async Task HandleAsync_RejectsDatesOutsideTheRetainedMetricsWindow(
        int year,
        int month,
        int day)
    {
        Mock<IWatchPilotMetricsRepository> repository = new(MockBehavior.Strict);
        GetWatchPilotMetricsQueryHandler handler = new(
            repository.Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));

        ApplicationResult<WatchPilotMetricsResult> result = await handler.HandleAsync(
            new GetWatchPilotMetricsQuery(
                null,
                new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_RejectsAStartBeforeEmailAttemptRetention()
    {
        Mock<IWatchPilotMetricsRepository> repository = new(MockBehavior.Strict);
        GetWatchPilotMetricsQueryHandler handler = new(
            repository.Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));

        ApplicationResult<WatchPilotMetricsResult> result = await handler.HandleAsync(
            new GetWatchPilotMetricsQuery(
                new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.VerifyNoOtherCalls();
    }

    private static WatchPilotMetricsSnapshot EmptySnapshot()
    {
        return new WatchPilotMetricsSnapshot(
            0,
            new Dictionary<string, long>(),
            0,
            0,
            0,
            0,
            0,
            0,
            0m,
            0,
            0,
            0,
            0,
            0,
            false,
            null,
            null,
            0,
            new Dictionary<string, long>(),
            Array.Empty<WatchPilotDailyMetrics>());
    }
}
