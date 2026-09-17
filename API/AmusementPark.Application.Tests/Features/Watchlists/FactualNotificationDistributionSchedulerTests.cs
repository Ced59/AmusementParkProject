using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class FactualNotificationDistributionSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_ShouldUseRecoverableCoalescingUntilReceiptExists()
    {
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IFactualChangeEventRepository> events = new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(false);
        jobs.Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.Kind == FactualNotificationDistributionJob.Kind
                    && request.NaturalKey == "watch-web:event-1:start"
                    && request.RequestedRevision == 0
                    && request.CorrelationId == "event-1"),
                CancellationToken.None))
            .ReturnsAsync(CreateJob());
        FactualNotificationDistributionScheduler scheduler = new FactualNotificationDistributionScheduler(
            jobs.Object,
            events.Object,
            receipts.Object);

        await scheduler.ScheduleAsync(" event-1 ", null, CancellationToken.None);

        jobs.VerifyAll();
        receipts.VerifyAll();
    }

    [Fact]
    public async Task ScheduleAsync_ShouldNotEnqueueCompletedDistribution()
    {
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IFactualChangeEventRepository> events = new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(true);
        FactualNotificationDistributionScheduler scheduler = new FactualNotificationDistributionScheduler(
            jobs.Object,
            events.Object,
            receipts.Object);

        await scheduler.ScheduleAsync("event-1", null, CancellationToken.None);

        jobs.VerifyNoOtherCalls();
    }

    private static DurableBackgroundJob CreateJob()
    {
        DateTime nowUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        return new DurableBackgroundJob(
            Id: "job-1",
            Kind: FactualNotificationDistributionJob.Kind,
            NaturalKey: "watch-web:event-1:start",
            IdempotencyKey: null,
            PayloadVersion: FactualNotificationDistributionJob.PayloadVersion,
            Payload: default,
            RequestedRevision: 0,
            ProcessedRevision: null,
            Status: DurableBackgroundJobStatus.Pending,
            Priority: 0,
            AttemptCount: 0,
            NotBeforeUtc: nowUtc,
            LeaseOwner: null,
            LeaseToken: null,
            LeaseExpiresAtUtc: null,
            CreatedAtUtc: nowUtc,
            UpdatedAtUtc: nowUtc,
            CompletedAtUtc: null,
            LastErrorCode: null,
            CorrelationId: "event-1");
    }
}
