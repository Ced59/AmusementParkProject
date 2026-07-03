using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Contracts;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Weather;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkWeather.Services;

public sealed class ParkWeatherRefreshStarterTests
{
    [Fact]
    public async Task StartManualFullRefreshAsync_WhenNoActiveRun_ShouldCreateQueuedRunAndCancelNextAutomaticDate()
    {
        ParkWeatherRefreshJob queuedJob = null!;
        ParkWeatherRun createdRun = null!;
        Mock<IParkWeatherRunRepository> runRepository = new Mock<IParkWeatherRunRepository>(MockBehavior.Strict);
        runRepository
            .Setup(repository => repository.HasActiveRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        runRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<ParkWeatherRun>(), It.IsAny<CancellationToken>()))
            .Callback<ParkWeatherRun, CancellationToken>((run, _) => createdRun = run)
            .ReturnsAsync((ParkWeatherRun run, CancellationToken _) =>
            {
                run.Id = "run-1";
                return run;
            });
        Mock<IParkWeatherRefreshQueue> queue = new Mock<IParkWeatherRefreshQueue>(MockBehavior.Strict);
        queue
            .Setup(refreshQueue => refreshQueue.EnqueueAsync(It.IsAny<ParkWeatherRefreshJob>(), It.IsAny<CancellationToken>()))
            .Callback<ParkWeatherRefreshJob, CancellationToken>((job, _) => queuedJob = job)
            .Returns(ValueTask.CompletedTask);
        ParkWeatherRefreshStarter starter = new ParkWeatherRefreshStarter(
            runRepository.Object,
            queue.Object,
            new TestRefreshSettings());

        ApplicationResult<ParkWeatherRunResult> result = await starter.StartManualFullRefreshAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        ParkWeatherRunResult resultValue = result.Value ?? throw new InvalidOperationException("A successful result should expose the created run.");
        Assert.Equal("run-1", queuedJob.RunId);
        Assert.Equal(ParkWeatherRunTrigger.Manual, createdRun.Trigger);
        Assert.Equal(ParkWeatherRefreshScope.FullVisibleParks, createdRun.Scope);
        Assert.Equal(ParkWeatherRunStatus.Queued, createdRun.Status);
        Assert.NotNull(createdRun.CancelsAutomaticRunLocalDate);
        Assert.Equal(createdRun.CancelsAutomaticRunLocalDate, resultValue.CancelsAutomaticRunLocalDate);
        runRepository.VerifyAll();
        queue.VerifyAll();
    }

    [Fact]
    public async Task StartManualFullRefreshAsync_WhenActiveRunExists_ShouldFailWithoutQueueing()
    {
        Mock<IParkWeatherRunRepository> runRepository = new Mock<IParkWeatherRunRepository>(MockBehavior.Strict);
        runRepository
            .Setup(repository => repository.HasActiveRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IParkWeatherRefreshQueue> queue = new Mock<IParkWeatherRefreshQueue>(MockBehavior.Strict);
        ParkWeatherRefreshStarter starter = new ParkWeatherRefreshStarter(
            runRepository.Object,
            queue.Object,
            new TestRefreshSettings());

        ApplicationResult<ParkWeatherRunResult> result = await starter.StartManualFullRefreshAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-weather.run-active");
        runRepository.VerifyAll();
        queue.Verify(refreshQueue => refreshQueue.EnqueueAsync(It.IsAny<ParkWeatherRefreshJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartManualFullRefreshAsync_WhenQueueFailsAfterRunCreation_ShouldMarkRunFailed()
    {
        ParkWeatherRun createdRun = null!;
        ParkWeatherRun updatedRun = null!;
        Mock<IParkWeatherRunRepository> runRepository = new Mock<IParkWeatherRunRepository>(MockBehavior.Strict);
        runRepository
            .Setup(repository => repository.HasActiveRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        runRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<ParkWeatherRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkWeatherRun run, CancellationToken _) =>
            {
                run.Id = "run-1";
                createdRun = run;
                return run;
            });
        runRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<ParkWeatherRun>(), It.IsAny<CancellationToken>()))
            .Callback<ParkWeatherRun, CancellationToken>((run, _) => updatedRun = run)
            .Returns(Task.CompletedTask);
        Mock<IParkWeatherRefreshQueue> queue = new Mock<IParkWeatherRefreshQueue>(MockBehavior.Strict);
        queue
            .Setup(refreshQueue => refreshQueue.EnqueueAsync(It.IsAny<ParkWeatherRefreshJob>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Queue is unavailable."));
        ParkWeatherRefreshStarter starter = new ParkWeatherRefreshStarter(
            runRepository.Object,
            queue.Object,
            new TestRefreshSettings());

        ApplicationResult<ParkWeatherRunResult> result = await starter.StartManualFullRefreshAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-weather.queue-unavailable");
        Assert.Same(createdRun, updatedRun);
        Assert.Equal(ParkWeatherRunStatus.Failed, updatedRun.Status);
        Assert.NotNull(updatedRun.CompletedAtUtc);
        Assert.Equal("Weather refresh could not be queued.", updatedRun.Message);
        runRepository.VerifyAll();
        queue.VerifyAll();
    }

    private sealed class TestRefreshSettings : IParkWeatherRefreshSettings
    {
        public bool IsAutomaticRefreshEnabled => true;

        public int ForecastDays => 7;

        public int ForecastPastRetentionDays => 3;

        public bool IncludeYesterdayObservation => true;

        public int HistoricalBackfillYears => 0;

        public int HistoricalComparisonYearsLimit => 10;

        public int DelayBetweenParksMilliseconds => 0;

        public string AutomaticRefreshTimeZoneId => "UTC";

        public int AutomaticRefreshHour => 2;

        public int AutomaticRefreshMinute => 15;
    }
}
