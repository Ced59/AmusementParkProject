using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.FactualEvents;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.FactualEvents.Services;
using AmusementPark.Core.Domain.FactualEvents;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FactualEvents.Services;

public sealed class FactualChangeMaterializationJobHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithInvalidPayload_ShouldDeadLetterWithoutReadingSource()
    {
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        FactualChangeMaterializationJobHandler handler =
            new FactualChangeMaterializationJobHandler(outbox.Object, events.Object);
        DurableBackgroundJobExecutionContext context = CreateContext(
            JsonSerializer.SerializeToElement(new { invalid = true }));

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            context,
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        Assert.Equal(FactualChangeMaterializationErrorCodes.InvalidPayload, result.ErrorCode);
        outbox.VerifyNoOtherCalls();
        events.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithPendingSource_ShouldCreateOneDraftAndAcknowledgeIt()
    {
        FactualChangeOutboxEntry entry = CreateEntry();
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        outbox.Setup(value => value.GetAsync(entry.Id, CancellationToken.None))
            .ReturnsAsync(entry);
        outbox.Setup(value => value.MarkMaterializedAsync(
                entry.Id,
                entry.EventId,
                entry.Version,
                It.Is<DateTime>(timestamp => timestamp.Kind == DateTimeKind.Utc),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        events.Setup(value => value.CreateAsync(
                It.Is<FactualChangeEvent>(factualEvent =>
                    factualEvent.Id.Value == entry.EventId
                    && factualEvent.Status == FactualChangeStatus.Draft
                    && factualEvent.Revision == entry.SourceRevision),
                CancellationToken.None))
            .ReturnsAsync(FactualChangeEventWriteDisposition.Created);
        FactualChangeMaterializationJobHandler handler =
            new FactualChangeMaterializationJobHandler(outbox.Object, events.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(CreatePayload(entry)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        outbox.VerifyAll();
        events.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_AfterDuplicateDelivery_ShouldNotCreateASecondEvent()
    {
        FactualChangeOutboxEntry pending = CreateEntry();
        FactualChangeOutboxEntry materialized = pending with
        {
            MaterializedAtUtc = pending.RecordedAtUtc.AddMinutes(1),
            Version = 2,
        };
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        outbox.Setup(value => value.GetAsync(materialized.Id, CancellationToken.None))
            .ReturnsAsync(materialized);
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        FactualChangeMaterializationJobHandler handler =
            new FactualChangeMaterializationJobHandler(outbox.Object, events.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(CreatePayload(materialized)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        outbox.VerifyAll();
        events.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenLogicalRevisionConflicts_ShouldDeadLetter()
    {
        FactualChangeOutboxEntry entry = CreateEntry();
        Mock<IFactualChangeOutboxRepository> outbox =
            new Mock<IFactualChangeOutboxRepository>(MockBehavior.Strict);
        outbox.Setup(value => value.GetAsync(entry.Id, CancellationToken.None))
            .ReturnsAsync(entry);
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        events.Setup(value => value.CreateAsync(
                It.IsAny<FactualChangeEvent>(),
                CancellationToken.None))
            .ReturnsAsync(FactualChangeEventWriteDisposition.Conflict);
        FactualChangeMaterializationJobHandler handler =
            new FactualChangeMaterializationJobHandler(outbox.Object, events.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(CreatePayload(entry)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        Assert.Equal(FactualChangeMaterializationErrorCodes.EventConflict, result.ErrorCode);
        outbox.VerifyAll();
        events.VerifyAll();
    }

    private static JsonElement CreatePayload(FactualChangeOutboxEntry entry)
    {
        return JsonSerializer.SerializeToElement(
            new FactualChangeMaterializationJobPayload(entry.Id, entry.SourceRevision));
    }

    private static DurableBackgroundJobExecutionContext CreateContext(JsonElement payload)
    {
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            FactualChangeMaterializationJob.PayloadVersion,
            payload,
            null,
            1,
            null);
    }

    private static FactualChangeOutboxEntry CreateEntry()
    {
        DateTime nowUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        return new FactualChangeOutboxEntry(
            "outbox-1",
            "event-1",
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
            "park:park-1:name",
            7,
            nowUtc,
            null,
            null,
            null,
            1);
    }
}
