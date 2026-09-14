using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class GetParkFitPilotMetricsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldAggregateDailyCountersIntoBusinessRates()
    {
        DateTime fromUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)
            .AddTicks(-1);
        ParkFitPilotDailyMetrics day = new(
            "2026-09-14",
            new Dictionary<string, long>
            {
                ["SearchStarted"] = 10,
                ["SearchCompleted"] = 8,
                ["ExplanationViewed"] = 3,
                ["ComparisonOpened"] = 2,
            },
            new Dictionary<string, long> { ["None"] = 1, ["FiveOrMore"] = 7 },
            new Dictionary<string, long> { ["Significant"] = 1 },
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            2,
            1);
        Mock<IParkFitPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.ReadAsync(fromUtc, toUtc, CancellationToken.None))
            .ReturnsAsync(new ParkFitPilotMetricsSnapshot([day]));
        GetParkFitPilotMetricsQueryHandler handler = new(
            repository.Object,
            new FixedParkFitPilotTimeProvider(
                new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero)));

        ApplicationResult<ParkFitPilotMetricsResult> result = await handler.HandleAsync(
            new GetParkFitPilotMetricsQuery(fromUtc, toUtc),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(80m, result.Value.Health.CompletionRatePercent);
        Assert.Equal(12.5m, result.Value.Health.NoResultRatePercent);
        Assert.Equal(ParkFitPilotSignal.Encouraging, result.Value.Health.Signal);
        Assert.Equal(2, result.Value.SourceReports);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAnInvertedDateRange()
    {
        Mock<IParkFitPilotMetricsRepository> repository = new(MockBehavior.Strict);
        GetParkFitPilotMetricsQueryHandler handler = new(repository.Object);

        ApplicationResult<ParkFitPilotMetricsResult> result = await handler.HandleAsync(
            new GetParkFitPilotMetricsQuery(
                new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithoutBounds_ShouldReadExactlyThirtyInclusiveUtcDays()
    {
        DateTime expectedFromUtc = new(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);
        DateTime expectedToUtc = new(2026, 9, 15, 23, 59, 59, 999, DateTimeKind.Utc);
        expectedToUtc = expectedToUtc.AddTicks(9999);
        Mock<IParkFitPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.ReadAsync(
                expectedFromUtc,
                expectedToUtc,
                CancellationToken.None))
            .ReturnsAsync(new ParkFitPilotMetricsSnapshot([]));
        GetParkFitPilotMetricsQueryHandler handler = new(
            repository.Object,
            new FixedParkFitPilotTimeProvider(
                new DateTimeOffset(2026, 9, 15, 12, 34, 56, TimeSpan.Zero)));

        ApplicationResult<ParkFitPilotMetricsResult> result = await handler.HandleAsync(
            new GetParkFitPilotMetricsQuery(null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedFromUtc, result.Value.FromUtc);
        Assert.Equal(expectedToUtc, result.Value.ToUtc);
        repository.VerifyAll();
    }
}
