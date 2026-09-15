using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.FactualEvents.Services;
using AmusementPark.Core.Domain.FactualEvents;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FactualEvents.Services;

public sealed class FactualChangeCaptureServiceTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CaptureAfterCommitAsync_WithoutStructuredChange_ShouldNotPersistOrSchedule()
    {
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        Mock<IFactualChangeMaterializationScheduler> scheduler =
            new Mock<IFactualChangeMaterializationScheduler>(MockBehavior.Strict);
        FactualChangeCaptureService service = CreateService(outbox, scheduler);
        FactValue value = FactValue.FromText("Parc exemple");

        FactualChangeCaptureResult result = await service.CaptureAfterCommitAsync(
            CreateRequest(value, value),
            CancellationToken.None);

        Assert.Equal(FactualChangeCaptureDisposition.NoChange, result.Disposition);
        outbox.VerifyNoOtherCalls();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CaptureAfterCommitAsync_WithNewRevision_ShouldPersistBeforeScheduling()
    {
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        Mock<IFactualChangeMaterializationScheduler> scheduler =
            new Mock<IFactualChangeMaterializationScheduler>(MockBehavior.Strict);
        outbox.Setup(value => value.RecordAsync(
                It.IsAny<FactualChangeOutboxEntry>(),
                CancellationToken.None))
            .ReturnsAsync((FactualChangeOutboxEntry entry, CancellationToken _) =>
                new FactualChangeOutboxWriteResult(
                    FactualChangeOutboxWriteDisposition.Created,
                    entry));
        scheduler.Setup(value => value.ScheduleAsync(
                It.IsAny<FactualChangeOutboxEntry>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        FactualChangeCaptureService service = CreateService(outbox, scheduler);

        FactualChangeCaptureResult result = await service.CaptureAfterCommitAsync(
            CreateRequest(FactValue.FromText("Ancien nom"), FactValue.FromText("Nouveau nom")),
            CancellationToken.None);

        Assert.Equal(FactualChangeCaptureDisposition.Scheduled, result.Disposition);
        Assert.NotNull(result.OutboxEntryId);
        Assert.NotNull(result.EventId);
        outbox.VerifyAll();
        scheduler.VerifyAll();
    }

    [Fact]
    public async Task CaptureAfterCommitAsync_WhenSchedulingFails_ShouldKeepRecoverableSourceState()
    {
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        Mock<IFactualChangeMaterializationScheduler> scheduler =
            new Mock<IFactualChangeMaterializationScheduler>(MockBehavior.Strict);
        outbox.Setup(value => value.RecordAsync(
                It.IsAny<FactualChangeOutboxEntry>(),
                CancellationToken.None))
            .ReturnsAsync((FactualChangeOutboxEntry entry, CancellationToken _) =>
                new FactualChangeOutboxWriteResult(
                    FactualChangeOutboxWriteDisposition.Created,
                    entry));
        scheduler.Setup(value => value.ScheduleAsync(
                It.IsAny<FactualChangeOutboxEntry>(),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable."));
        FactualChangeCaptureService service = CreateService(outbox, scheduler);

        FactualChangeCaptureResult result = await service.CaptureAfterCommitAsync(
            CreateRequest(FactValue.FromText("Ancien nom"), FactValue.FromText("Nouveau nom")),
            CancellationToken.None);

        Assert.Equal(
            FactualChangeCaptureDisposition.RecordedPendingScheduling,
            result.Disposition);
        Assert.NotNull(result.OutboxEntryId);
        outbox.VerifyAll();
        scheduler.VerifyAll();
    }

    [Fact]
    public async Task CaptureAfterCommitAsync_WithConflictingLogicalRevision_ShouldNotSchedule()
    {
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        Mock<IFactualChangeMaterializationScheduler> scheduler =
            new Mock<IFactualChangeMaterializationScheduler>(MockBehavior.Strict);
        outbox.Setup(value => value.RecordAsync(
                It.IsAny<FactualChangeOutboxEntry>(),
                CancellationToken.None))
            .ReturnsAsync(new FactualChangeOutboxWriteResult(
                FactualChangeOutboxWriteDisposition.Conflict,
                null));
        FactualChangeCaptureService service = CreateService(outbox, scheduler);

        FactualChangeCaptureResult result = await service.CaptureAfterCommitAsync(
            CreateRequest(FactValue.FromText("Ancien nom"), FactValue.FromText("Nouveau nom")),
            CancellationToken.None);

        Assert.Equal(FactualChangeCaptureDisposition.Conflict, result.Disposition);
        outbox.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    private static FactualChangeCaptureService CreateService(
        Mock<IFactualChangeOutboxRepository> outbox,
        Mock<IFactualChangeMaterializationScheduler> scheduler)
    {
        return new FactualChangeCaptureService(
            outbox.Object,
            scheduler.Object,
            NullLogger<FactualChangeCaptureService>.Instance);
    }

    private static FactualChangeCaptureRequest CreateRequest(
        FactValue? previousValue,
        FactValue? newValue)
    {
        return new FactualChangeCaptureRequest(
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            previousValue,
            newValue,
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Nom officiel",
                "https://example.com/news",
                RecordedAtUtc.AddHours(-2)),
            DataConfidence.High,
            RecordedAtUtc.AddHours(-1),
            "park:park-1:name",
            7,
            RecordedAtUtc);
    }
}
