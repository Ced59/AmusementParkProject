using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public sealed class FactualChangeCaptureService
{
    private readonly IFactualChangeOutboxRepository outboxRepository;
    private readonly IFactualChangeMaterializationScheduler scheduler;
    private readonly ILogger<FactualChangeCaptureService> logger;

    public FactualChangeCaptureService(
        IFactualChangeOutboxRepository outboxRepository,
        IFactualChangeMaterializationScheduler scheduler,
        ILogger<FactualChangeCaptureService> logger)
    {
        this.outboxRepository = outboxRepository;
        this.scheduler = scheduler;
        this.logger = logger;
    }

    public async Task<FactualChangeCaptureResult> CaptureAfterCommitAsync(
        FactualChangeCaptureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourceRevision < 1 || request.SourceRevision > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "The source revision must be positive.");
        }

        FactualChangeDiff? diff = FactualChangeDiff.Detect(
            request.PreviousValue,
            request.NewValue);
        if (diff is null)
        {
            return new FactualChangeCaptureResult(
                FactualChangeCaptureDisposition.NoChange,
                null,
                null);
        }

        FactualChangeEvent candidate = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.New(),
            request.Type,
            request.Target,
            diff.PreviousValue,
            diff.NewValue,
            request.Source,
            request.Confidence,
            request.OccurredAtUtc,
            request.DeduplicationKey,
            checked((int)request.SourceRevision),
            request.RecordedAtUtc);
        FactualChangeOutboxEntry newEntry = new FactualChangeOutboxEntry(
            Guid.NewGuid().ToString("N"),
            candidate.Id.Value,
            candidate.Type,
            candidate.DefinitionVersion,
            candidate.Target,
            candidate.PreviousValue,
            candidate.NewValue,
            candidate.Source,
            candidate.Confidence,
            candidate.OccurredAtUtc,
            candidate.DeduplicationKey,
            request.SourceRevision,
            candidate.CreatedAtUtc,
            null,
            1);
        FactualChangeOutboxWriteResult write = await this.outboxRepository.RecordAsync(
            newEntry,
            cancellationToken);
        if (write.Disposition == FactualChangeOutboxWriteDisposition.Conflict
            || write.Entry is null)
        {
            return new FactualChangeCaptureResult(
                FactualChangeCaptureDisposition.Conflict,
                null,
                null);
        }

        FactualChangeOutboxEntry recordedEntry = write.Entry;
        try
        {
            await this.scheduler.ScheduleAsync(recordedEntry, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Factual outbox entry {OutboxEntryId} was recorded but could not be scheduled immediately.",
                recordedEntry.Id);
            return new FactualChangeCaptureResult(
                FactualChangeCaptureDisposition.RecordedPendingScheduling,
                recordedEntry.Id,
                recordedEntry.EventId);
        }

        return new FactualChangeCaptureResult(
            write.Disposition == FactualChangeOutboxWriteDisposition.Created
                ? FactualChangeCaptureDisposition.Scheduled
                : FactualChangeCaptureDisposition.AlreadyRecorded,
            recordedEntry.Id,
            recordedEntry.EventId);
    }
}
