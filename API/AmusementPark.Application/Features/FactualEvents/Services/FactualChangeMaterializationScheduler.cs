using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public sealed class FactualChangeMaterializationScheduler : IFactualChangeMaterializationScheduler
{
    public const int MaximumReconciliationBatchSize = 100;

    private readonly IFactualChangeOutboxRepository outboxRepository;
    private readonly IDurableBackgroundJobRepository jobRepository;
    private readonly ILogger<FactualChangeMaterializationScheduler> logger;
    private readonly TimeProvider timeProvider;

    public FactualChangeMaterializationScheduler(
        IFactualChangeOutboxRepository outboxRepository,
        IDurableBackgroundJobRepository jobRepository,
        ILogger<FactualChangeMaterializationScheduler> logger)
        : this(outboxRepository, jobRepository, logger, TimeProvider.System)
    {
    }

    internal FactualChangeMaterializationScheduler(
        IFactualChangeOutboxRepository outboxRepository,
        IDurableBackgroundJobRepository jobRepository,
        ILogger<FactualChangeMaterializationScheduler> logger,
        TimeProvider timeProvider)
    {
        this.outboxRepository = outboxRepository;
        this.jobRepository = jobRepository;
        this.logger = logger;
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task ScheduleAsync(
        FactualChangeOutboxEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.IsMaterialized || entry.IsTerminal)
        {
            return;
        }

        FactualChangeMaterializationJobPayload payload =
            new FactualChangeMaterializationJobPayload(entry.Id, entry.SourceRevision);
        EnqueueExactBackgroundJobRequest request = new EnqueueExactBackgroundJobRequest(
            FactualChangeMaterializationJob.Kind,
            FactualChangeMaterializationJob.BuildIdempotencyKey(
                entry.DeduplicationKey,
                entry.SourceRevision),
            FactualChangeMaterializationJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            CorrelationId: entry.EventId);
        DurableBackgroundJob job = await this.jobRepository.EnqueueExactAsync(
            request,
            cancellationToken);
        if (!IsTerminalForScheduling(job.Status))
        {
            return;
        }

        FactualChangeOutboxEntry? currentEntry = await this.outboxRepository.GetAsync(
            entry.Id,
            cancellationToken);
        if (currentEntry?.IsMaterialized == true || currentEntry?.IsTerminal == true)
        {
            return;
        }

        string errorCode = job.LastErrorCode?.Trim() ?? string.Empty;
        if (errorCode.Length == 0)
        {
            errorCode = $"factual-materialization.{job.Status.ToString().ToLowerInvariant()}";
        }

        DateTime terminalAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        bool markedTerminal = await this.outboxRepository.MarkTerminalAsync(
            entry.Id,
            entry.EventId,
            entry.Version,
            terminalAtUtc,
            errorCode,
            cancellationToken);
        if (!markedTerminal)
        {
            currentEntry = await this.outboxRepository.GetAsync(entry.Id, cancellationToken);
            if (currentEntry?.IsMaterialized == true || currentEntry?.IsTerminal == true)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Unable to acknowledge terminal factual outbox entry '{entry.Id}'.");
        }

        throw new FactualChangeTerminalSchedulingException(job.Status, errorCode);
    }

    public async Task<FactualChangeOutboxCursor?> ReconcilePendingAsync(
        FactualChangeOutboxCursor? after,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FactualChangeOutboxEntry> pendingEntries =
            await this.outboxRepository.ListPendingAsync(
                after,
                MaximumReconciliationBatchSize,
                cancellationToken);
        foreach (FactualChangeOutboxEntry entry in pendingEntries)
        {
            try
            {
                await this.ScheduleAsync(entry, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile factual outbox entry {OutboxEntryId}; remaining entries will continue.",
                    entry.Id);
            }
        }

        return BuildNextCursor(pendingEntries);
    }

    internal static FactualChangeOutboxCursor? BuildNextCursor(
        IReadOnlyCollection<FactualChangeOutboxEntry> pendingEntries)
    {
        if (pendingEntries.Count < MaximumReconciliationBatchSize)
        {
            return null;
        }

        FactualChangeOutboxEntry lastEntry = pendingEntries.Last();
        return new FactualChangeOutboxCursor(lastEntry.RecordedAtUtc, lastEntry.Id);
    }

    private static bool IsTerminalForScheduling(DurableBackgroundJobStatus status)
    {
        return status is DurableBackgroundJobStatus.Succeeded
            or DurableBackgroundJobStatus.DeadLetter
            or DurableBackgroundJobStatus.Cancelled
            or DurableBackgroundJobStatus.Superseded;
    }
}
