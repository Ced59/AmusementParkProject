using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class FactualNotificationDistributionScheduler : IFactualNotificationDistributionScheduler
{
    public const int ReconciliationBatchSize = 100;

    private readonly IDurableBackgroundJobRepository jobRepository;
    private readonly IFactualChangeEventRepository eventRepository;
    private readonly IFactualNotificationDistributionReceiptRepository receiptRepository;

    public FactualNotificationDistributionScheduler(
        IDurableBackgroundJobRepository jobRepository,
        IFactualChangeEventRepository eventRepository,
        IFactualNotificationDistributionReceiptRepository receiptRepository)
    {
        this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
    }

    public async Task ScheduleAsync(
        string eventId,
        string? afterSubscriptionId,
        CancellationToken cancellationToken)
    {
        string normalizedEventId = eventId?.Trim() ?? string.Empty;
        if (normalizedEventId.Length == 0)
        {
            throw new ArgumentException("An event identifier is required.", nameof(eventId));
        }

        if (await this.receiptRepository.IsCompletedAsync(normalizedEventId, cancellationToken))
        {
            return;
        }

        string normalizedCursor = string.IsNullOrWhiteSpace(afterSubscriptionId)
            ? "start"
            : afterSubscriptionId.Trim();
        FactualNotificationDistributionJobPayload payload = new FactualNotificationDistributionJobPayload(
            normalizedEventId,
            string.Equals(normalizedCursor, "start", StringComparison.Ordinal)
                ? null
                : normalizedCursor);
        await this.jobRepository.CoalesceAsync(
            new CoalesceBackgroundJobRequest(
                FactualNotificationDistributionJob.Kind,
                $"watch-web:{normalizedEventId}:{normalizedCursor}",
                RequestedRevision: 0,
                FactualNotificationDistributionJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                CorrelationId: normalizedEventId),
            cancellationToken);
    }

    public async Task<PublishedFactualEventCursor?> ReconcileAsync(
        PublishedFactualEventCursor? after,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FactualChangeEvent> published =
            await this.eventRepository.ListPublishedAsync(
                after,
                ReconciliationBatchSize,
                cancellationToken);
        foreach (FactualChangeEvent factualEvent in published)
        {
            if (!await this.receiptRepository.IsCompletedAsync(factualEvent.Id.Value, cancellationToken))
            {
                await this.ScheduleAsync(factualEvent.Id.Value, null, cancellationToken);
            }
        }

        if (published.Count < ReconciliationBatchSize)
        {
            return null;
        }

        FactualChangeEvent last = published.Last();
        return new PublishedFactualEventCursor(
            last.PublishedAtUtc
                ?? throw new InvalidOperationException("A published event must have a publication date."),
            last.Id.Value);
    }
}
