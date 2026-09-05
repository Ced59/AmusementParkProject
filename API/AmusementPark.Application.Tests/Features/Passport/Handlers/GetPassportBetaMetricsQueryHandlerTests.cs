using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class GetPassportBetaMetricsQueryHandlerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 5, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_ShouldCalculateARepeatUsageCandidateWithoutExposingUsers()
    {
        Mock<IPassportBetaMetricsSource> source =
            new Mock<IPassportBetaMetricsSource>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        DateTime fromUtc = NowUtc.AddDays(-30);
        IReadOnlyCollection<PassportBetaDailyMetrics> daily =
        [
            new PassportBetaDailyMetrics("2026-09-05", 4, 2, 1),
        ];
        source.Setup(value => value.ReadAsync(
                fromUtc,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new PassportBetaMetricsSourceSnapshot(
                12,
                10,
                8,
                3,
                daily));
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        GetPassportBetaMetricsQueryHandler handler = new GetPassportBetaMetricsQueryHandler(
            source.Object,
            clock.Object);

        ApplicationResult<PassportBetaMetricsResult> result = await handler.HandleAsync(
            new GetPassportBetaMetricsQuery(null, null));

        Assert.True(result.IsSuccess);
        PassportBetaMetricsResult metrics = Assert.IsType<PassportBetaMetricsResult>(
            result.Value);
        Assert.Equal(37.5m, metrics.RepeatUsageRatePercent);
        Assert.Equal(PassportBetaRepeatUsageSignal.Candidate, metrics.RepeatUsageSignal);
        Assert.True(metrics.RequiresQualitativeValidation);
        Assert.Same(daily, metrics.Daily);
        Assert.Null(typeof(PassportBetaMetricsResult).GetProperty("UserId"));
        Assert.Null(typeof(PassportBetaDailyMetrics).GetProperty("UserId"));
        source.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithOneReturningUser_ShouldReportAnEmergingSignal()
    {
        Mock<IPassportBetaMetricsSource> source =
            new Mock<IPassportBetaMetricsSource>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        source.Setup(value => value.ReadAsync(
                NowUtc.AddDays(-10),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new PassportBetaMetricsSourceSnapshot(
                2,
                2,
                2,
                1,
                Array.Empty<PassportBetaDailyMetrics>()));
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        GetPassportBetaMetricsQueryHandler handler = new GetPassportBetaMetricsQueryHandler(
            source.Object,
            clock.Object);

        ApplicationResult<PassportBetaMetricsResult> result = await handler.HandleAsync(
            new GetPassportBetaMetricsQuery(NowUtc.AddDays(-10), NowUtc));

        PassportBetaMetricsResult metrics = Assert.IsType<PassportBetaMetricsResult>(
            result.Value);
        Assert.Equal(PassportBetaRepeatUsageSignal.Emerging, metrics.RepeatUsageSignal);
        Assert.Equal(50m, metrics.RepeatUsageRatePercent);
        source.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldClampTheRangeToOneHundredAndEightyDays()
    {
        Mock<IPassportBetaMetricsSource> source =
            new Mock<IPassportBetaMetricsSource>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        source.Setup(value => value.ReadAsync(
                NowUtc.AddDays(-180),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new PassportBetaMetricsSourceSnapshot(
                0,
                0,
                0,
                0,
                Array.Empty<PassportBetaDailyMetrics>()));
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        GetPassportBetaMetricsQueryHandler handler = new GetPassportBetaMetricsQueryHandler(
            source.Object,
            clock.Object);

        ApplicationResult<PassportBetaMetricsResult> result = await handler.HandleAsync(
            new GetPassportBetaMetricsQuery(NowUtc.AddDays(-365), NowUtc));

        PassportBetaMetricsResult metrics = Assert.IsType<PassportBetaMetricsResult>(
            result.Value);
        Assert.Equal(NowUtc.AddDays(-180), metrics.FromUtc);
        Assert.Equal(PassportBetaRepeatUsageSignal.NotObserved, metrics.RepeatUsageSignal);
        Assert.Equal(0m, metrics.RepeatUsageRatePercent);
        source.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithAnInvertedRange_ShouldFailWithoutReadingData()
    {
        Mock<IPassportBetaMetricsSource> source =
            new Mock<IPassportBetaMetricsSource>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        GetPassportBetaMetricsQueryHandler handler = new GetPassportBetaMetricsQueryHandler(
            source.Object,
            clock.Object);

        ApplicationResult<PassportBetaMetricsResult> result = await handler.HandleAsync(
            new GetPassportBetaMetricsQuery(NowUtc, NowUtc.AddDays(-1)));

        Assert.False(result.IsSuccess);
        Assert.Equal("passport-beta.date-range-invalid", Assert.Single(result.Errors).Code);
        source.VerifyNoOtherCalls();
        clock.VerifyAll();
    }
}
