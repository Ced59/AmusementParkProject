using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
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
            CreateContext(new VisitPurgeJobPayload("visit-1", "owner-1")),
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
            CreateContext(new VisitPurgeJobPayload("visit-1", "owner-1")),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        store.VerifyAll();
    }

    private static VisitPurgeJobHandler CreateHandler(IVisitDeletionStore store)
    {
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        return new VisitPurgeJobHandler(store, clock.Object);
    }

    private static DurableBackgroundJobExecutionContext CreateContext(
        VisitPurgeJobPayload payload)
    {
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            VisitPurgeJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            null,
            1,
            null);
    }
}
