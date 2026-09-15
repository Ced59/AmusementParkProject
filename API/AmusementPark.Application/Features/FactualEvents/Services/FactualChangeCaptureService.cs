using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public sealed class FactualChangeCaptureService : IFactualChangeCaptureService
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
        FactualChangeOutboxEntry? newEntry = FactualChangeOutboxEntry.Create(request);
        if (newEntry is null)
        {
            return new FactualChangeCaptureResult(
                FactualChangeCaptureDisposition.NoChange,
                null,
                null);
        }

        return await this.CapturePreparedAfterCommitAsync(newEntry, cancellationToken);
    }

    public async Task<FactualChangeCaptureResult> CapturePreparedAfterCommitAsync(
        FactualChangeOutboxEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.SourceRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entry),
                "The source revision must be positive.");
        }

        FactualChangeOutboxWriteResult write = await this.outboxRepository.RecordAsync(
            entry,
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
        catch (FactualChangeTerminalSchedulingException exception)
        {
            this.logger.LogError(
                exception,
                "Factual outbox entry {OutboxEntryId} reached a terminal materialization job.",
                recordedEntry.Id);
            return new FactualChangeCaptureResult(
                FactualChangeCaptureDisposition.RecordedTerminalSchedulingFailure,
                recordedEntry.Id,
                recordedEntry.EventId);
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
