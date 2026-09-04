using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class VisitPurgeJobHandlerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WhenChildrenRemain_SchedulesABoundedRetry()
    {
        Mock<IVisitDeletionStore> store = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        store.Setup(value => value.PurgeBatchAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                NowUtc,
                VisitDeletionPolicy.PurgeBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionPurgeResult(false, 400));
        VisitPurgeJobHandler handler = CreateHandler(store.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new VisitPurgeJobPayload("visit-1", "owner-1", 2, 0)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Retry, result.Outcome);
        Assert.Equal("passport-visit-purge.remaining-documents", result.ErrorCode);
        store.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParentIsPurged_CompletesTheDurableJob()
    {
        Mock<IVisitDeletionStore> store = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        store.Setup(value => value.PurgeBatchAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                NowUtc,
                VisitDeletionPolicy.PurgeBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionPurgeResult(true, 1));
        VisitPurgeJobHandler handler = CreateHandler(store.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new VisitPurgeJobPayload("visit-1", "owner-1", 2, 0)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        store.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenTheAttemptBudgetIsHalfConsumed_ContinuesInANewJob()
    {
        Mock<IVisitDeletionStore> store = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        store.Setup(value => value.PurgeBatchAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                NowUtc,
                VisitDeletionPolicy.PurgeBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionPurgeResult(false, 400));
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(repository => repository.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == VisitPurgeJob.Kind
                    && request.IdempotencyKey == "passport-visit-purge:visit-1:2:4"
                    && request.Delay == null),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        VisitPurgeJobHandler handler = CreateHandler(store.Object, jobs.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(
                new VisitPurgeJobPayload("visit-1", "owner-1", 2, 3),
                attemptCount: 50),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        store.VerifyAll();
        jobs.VerifyAll();
    }

    private static VisitPurgeJobHandler CreateHandler(
        IVisitDeletionStore store,
        IDurableBackgroundJobRepository? jobs = null)
    {
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        IDurableBackgroundJobRepository jobRepository = jobs
            ?? new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict).Object;
        return new VisitPurgeJobHandler(
            store,
            new VisitPurgeScheduler(jobRepository),
            clock.Object);
    }

    private static DurableBackgroundJobExecutionContext CreateContext(
        VisitPurgeJobPayload payload,
        int attemptCount = 1)
    {
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            VisitPurgeJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            null,
            attemptCount,
            null);
    }
}
