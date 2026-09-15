using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public sealed class FactualChangeMaterializationJobHandler : IDurableBackgroundJobHandler
{
    private readonly IFactualChangeOutboxRepository outboxRepository;
    private readonly IFactualChangeEventRepository eventRepository;
    private readonly TimeProvider timeProvider;

    public FactualChangeMaterializationJobHandler(
        IFactualChangeOutboxRepository outboxRepository,
        IFactualChangeEventRepository eventRepository)
        : this(outboxRepository, eventRepository, TimeProvider.System)
    {
    }

    internal FactualChangeMaterializationJobHandler(
        IFactualChangeOutboxRepository outboxRepository,
        IFactualChangeEventRepository eventRepository,
        TimeProvider timeProvider)
    {
        this.outboxRepository = outboxRepository;
        this.eventRepository = eventRepository;
        this.timeProvider = timeProvider;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            FactualChangeMaterializationJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { FactualChangeMaterializationJob.PayloadVersion },
            TimeSpan.FromMinutes(2),
            maximumAttempts: 5,
            initialRetryDelay: TimeSpan.FromSeconds(30),
            maximumRetryDelay: TimeSpan.FromMinutes(10),
            maximumConcurrency: 2);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        FactualChangeMaterializationJobPayload? payload = ParsePayload(context);
        if (payload is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualChangeMaterializationErrorCodes.InvalidPayload);
        }

        FactualChangeOutboxEntry? entry = await this.outboxRepository.GetAsync(
            payload.OutboxEntryId,
            cancellationToken);
        if (entry is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualChangeMaterializationErrorCodes.SourceMissing);
        }

        if (entry.SourceRevision != payload.SourceRevision)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualChangeMaterializationErrorCodes.SourceConflict);
        }

        if (entry.SourceRevision < 1)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualChangeMaterializationErrorCodes.SourceConflict);
        }

        if (entry.IsMaterialized)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        FactualChangeEvent factualEvent;
        try
        {
            factualEvent = FactualChangeEvent.Restore(
                FactualChangeEventId.Parse(entry.EventId),
                entry.Type,
                entry.DefinitionVersion,
                entry.Target,
                entry.PreviousValue,
                entry.NewValue,
                entry.Source,
                entry.Confidence,
                entry.OccurredAtUtc,
                entry.DeduplicationKey,
                entry.SourceRevision,
                FactualChangeStatus.Draft,
                entry.RecordedAtUtc,
                entry.RecordedAtUtc,
                null,
                null,
                null,
                null,
                null,
                1);
        }
        catch (Exception exception)
            when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualChangeMaterializationErrorCodes.SourceConflict);
        }
        FactualChangeEventWriteDisposition write = await this.eventRepository.CreateAsync(
            factualEvent,
            cancellationToken);
        if (write == FactualChangeEventWriteDisposition.Conflict)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualChangeMaterializationErrorCodes.EventConflict);
        }

        DateTime materializedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        bool marked = await this.outboxRepository.MarkMaterializedAsync(
            entry.Id,
            entry.EventId,
            entry.Version,
            materializedAtUtc,
            cancellationToken);
        if (!marked)
        {
            FactualChangeOutboxEntry? current = await this.outboxRepository.GetAsync(
                entry.Id,
                cancellationToken);
            if (current is null || !current.IsMaterialized
                || !string.Equals(current.EventId, entry.EventId, StringComparison.Ordinal))
            {
                return DurableBackgroundJobHandlerResult.Retry(
                    FactualChangeMaterializationErrorCodes.AcknowledgementConflict);
            }
        }

        return DurableBackgroundJobHandlerResult.Success();
    }

    private static FactualChangeMaterializationJobPayload? ParsePayload(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != FactualChangeMaterializationJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            FactualChangeMaterializationJobPayload? payload =
                context.Payload.Deserialize<FactualChangeMaterializationJobPayload>();
            return payload is not null
                && !string.IsNullOrWhiteSpace(payload.OutboxEntryId)
                && payload.SourceRevision > 0
                    ? payload
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
