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

    public FactualChangeMaterializationScheduler(
        IFactualChangeOutboxRepository outboxRepository,
        IDurableBackgroundJobRepository jobRepository,
        ILogger<FactualChangeMaterializationScheduler> logger)
    {
        this.outboxRepository = outboxRepository;
        this.jobRepository = jobRepository;
        this.logger = logger;
    }

    public async Task ScheduleAsync(
        FactualChangeOutboxEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.IsMaterialized)
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
        await this.jobRepository.EnqueueExactAsync(request, cancellationToken);
    }

    public async Task ReconcilePendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FactualChangeOutboxEntry> pendingEntries =
            await this.outboxRepository.ListPendingAsync(
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
    }
}
