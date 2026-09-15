using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.FactualEvents.Services;
using AmusementPark.Core.Domain.FactualEvents;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FactualEvents.Services;

public sealed class FactualChangeMaterializationSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_ShouldUseABoundedStableIdempotencyKey()
    {
        FactualChangeOutboxEntry entry = CreateEntry(new string('a', 300));
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        EnqueueExactBackgroundJobRequest? captured = null;
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .Callback((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                captured = request)
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateJob(request));
        FactualChangeMaterializationScheduler scheduler =
            new FactualChangeMaterializationScheduler(
                outbox.Object,
                jobs.Object,
                NullLogger<FactualChangeMaterializationScheduler>.Instance);

        await scheduler.ScheduleAsync(entry, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(FactualChangeMaterializationJob.Kind, captured.Kind);
        Assert.True(captured.IdempotencyKey.Length <= 200);
        Assert.Equal(
            FactualChangeMaterializationJob.BuildIdempotencyKey(
                entry.DeduplicationKey,
                entry.SourceRevision),
            captured.IdempotencyKey);
        FactualChangeMaterializationJobPayload? payload =
            captured.Payload.Deserialize<FactualChangeMaterializationJobPayload>();
        Assert.Equal(entry.Id, payload?.OutboxEntryId);
        Assert.Equal(entry.SourceRevision, payload?.SourceRevision);
        jobs.VerifyAll();
        outbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcilePendingAsync_WhenOneEntryFails_ShouldContinueTheBoundedBatch()
    {
        FactualChangeOutboxEntry first = CreateEntry("park:park-1:name", "outbox-1");
        FactualChangeOutboxEntry second = CreateEntry("park:park-2:name", "outbox-2");
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        outbox.Setup(value => value.ListPendingAsync(
                FactualChangeMaterializationScheduler.MaximumReconciliationBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { first, second });
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.CorrelationId == first.EventId),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Temporary failure."));
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.CorrelationId == second.EventId),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateJob(request));
        FactualChangeMaterializationScheduler scheduler =
            new FactualChangeMaterializationScheduler(
                outbox.Object,
                jobs.Object,
                NullLogger<FactualChangeMaterializationScheduler>.Instance);

        await scheduler.ReconcilePendingAsync(CancellationToken.None);

        outbox.VerifyAll();
        jobs.VerifyAll();
    }

    private static FactualChangeOutboxEntry CreateEntry(
        string deduplicationKey,
        string id = "outbox-1")
    {
        DateTime nowUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        return new FactualChangeOutboxEntry(
            id,
            $"event-{id}",
            FactualEventType.ParkNameChanged,
            FactualEventCatalog.CurrentSchemaVersion,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Ancien nom"),
            FactValue.FromText("Nouveau nom"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Nom officiel",
                "https://example.com/news",
                nowUtc.AddHours(-2)),
            DataConfidence.High,
            nowUtc.AddHours(-1),
            deduplicationKey,
            7,
            nowUtc,
            null,
            1);
    }

    private static DurableBackgroundJob CreateJob(EnqueueExactBackgroundJobRequest request)
    {
        DateTime nowUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        return new DurableBackgroundJob(
            Id: "job-1",
            Kind: request.Kind,
            NaturalKey: null,
            IdempotencyKey: request.IdempotencyKey,
            PayloadVersion: request.PayloadVersion,
            Payload: request.Payload,
            RequestedRevision: null,
            ProcessedRevision: null,
            Status: DurableBackgroundJobStatus.Pending,
            Priority: request.Priority,
            AttemptCount: 0,
            NotBeforeUtc: nowUtc,
            LeaseOwner: null,
            LeaseToken: null,
            LeaseExpiresAtUtc: null,
            CreatedAtUtc: nowUtc,
            UpdatedAtUtc: nowUtc,
            CompletedAtUtc: null,
            LastErrorCode: null,
            CorrelationId: request.CorrelationId);
    }
}
