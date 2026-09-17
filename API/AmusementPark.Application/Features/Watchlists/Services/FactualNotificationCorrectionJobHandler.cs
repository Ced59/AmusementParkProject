using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class FactualNotificationCorrectionJobHandler : IDurableBackgroundJobHandler
{
    private const int MaximumCorrectionChainLength = 32;

    private readonly IFactualChangeEventRepository eventRepository;
    private readonly IUserNotificationRepository notificationRepository;
    private readonly UserNotificationCreationService notificationCreationService;
    private readonly IFactualNotificationDistributionReceiptRepository receiptRepository;
    private readonly IFactualNotificationDistributionScheduler scheduler;
    private readonly INotificationDigestScheduler digestScheduler;
    private readonly TimeProvider timeProvider;

    public FactualNotificationCorrectionJobHandler(
        IFactualChangeEventRepository eventRepository,
        IUserNotificationRepository notificationRepository,
        UserNotificationCreationService notificationCreationService,
        IFactualNotificationDistributionReceiptRepository receiptRepository,
        IFactualNotificationDistributionScheduler scheduler,
        INotificationDigestScheduler digestScheduler)
        : this(
            eventRepository,
            notificationRepository,
            notificationCreationService,
            receiptRepository,
            scheduler,
            digestScheduler,
            TimeProvider.System)
    {
    }

    internal FactualNotificationCorrectionJobHandler(
        IFactualChangeEventRepository eventRepository,
        IUserNotificationRepository notificationRepository,
        UserNotificationCreationService notificationCreationService,
        IFactualNotificationDistributionReceiptRepository receiptRepository,
        IFactualNotificationDistributionScheduler scheduler,
        INotificationDigestScheduler digestScheduler,
        TimeProvider timeProvider)
    {
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        this.notificationCreationService = notificationCreationService
            ?? throw new ArgumentNullException(nameof(notificationCreationService));
        this.receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        this.digestScheduler = digestScheduler ?? throw new ArgumentNullException(nameof(digestScheduler));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            FactualNotificationCorrectionJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { FactualNotificationCorrectionJob.PayloadVersion },
            TimeSpan.FromMinutes(2),
            maximumAttempts: 5,
            initialRetryDelay: TimeSpan.FromSeconds(30),
            maximumRetryDelay: TimeSpan.FromMinutes(10),
            maximumConcurrency: 2);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        FactualNotificationCorrectionJobPayload? payload = Parse(context);
        if (payload is null
            || payload.EventVersion < 1
            || !FactualChangeEventId.TryParse(payload.EventId, out FactualChangeEventId eventId))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.InvalidPayload);
        }

        string receiptId = FactualNotificationCorrectionJob.ReceiptId(eventId.Value, payload.EventVersion);
        if (await this.receiptRepository.IsCompletedAsync(receiptId, cancellationToken))
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        FactualChangeEvent? factualEvent = await this.eventRepository.GetAsync(eventId, cancellationToken);
        if (factualEvent is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.EventMissing);
        }

        if (factualEvent.Version != payload.EventVersion
            || factualEvent.Status is not FactualChangeStatus.Corrected and not FactualChangeStatus.Retracted
            || factualEvent.TerminalAtUtc is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.EventNotTerminal);
        }

        if (!await this.receiptRepository.IsCompletedAsync(factualEvent.Id.Value, cancellationToken))
        {
            return DurableBackgroundJobHandlerResult.Retry(
                FactualNotificationDistributionErrorCodes.InitialDistributionPending);
        }

        IReadOnlyCollection<UserNotification> originals =
            await this.notificationRepository.ListByFactualEventAsync(
                factualEvent.Id,
                payload.AfterNotificationId,
                FactualNotificationCorrectionJob.NotificationBatchSize,
                cancellationToken);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (factualEvent.Status == FactualChangeStatus.Corrected)
        {
            DurableBackgroundJobHandlerResult? failure = await this.DistributeCorrectionAsync(
                factualEvent,
                originals,
                nowUtc,
                cancellationToken);
            if (failure is not null)
            {
                return failure;
            }
        }
        else
        {
            await this.notificationRepository.RedeliverRetractionAsync(
                factualEvent.Id,
                originals.Select(static notification => notification.Id).ToArray(),
                factualEvent.TerminalAtUtc.Value,
                nowUtc < factualEvent.TerminalAtUtc.Value ? factualEvent.TerminalAtUtc.Value : nowUtc,
                cancellationToken);
            await this.digestScheduler.ScheduleAsync(
                factualEvent.Id,
                originals
                    .Select(static notification => notification.UserId)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                cancellationToken);
        }

        if (originals.Count == FactualNotificationCorrectionJob.NotificationBatchSize)
        {
            await this.scheduler.ScheduleCorrectionAsync(
                factualEvent.Id.Value,
                factualEvent.Version,
                originals.Last().Id.Value,
                cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }

        await this.receiptRepository.CompleteAsync(
            new FactualNotificationDistributionReceipt(receiptId, nowUtc),
            cancellationToken);
        return DurableBackgroundJobHandlerResult.Success();
    }

    private async Task<DurableBackgroundJobHandlerResult?> DistributeCorrectionAsync(
        FactualChangeEvent original,
        IReadOnlyCollection<UserNotification> originalNotifications,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        FactualChangeEvent? followUp = await this.ResolveLatestFollowUpAsync(original, cancellationToken);
        if (followUp is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.SupersedingEventMissing);
        }

        for (int chainAttempt = 0; chainAttempt < MaximumCorrectionChainLength; chainAttempt++)
        {
            try
            {
                UserNotification[] followUps = originalNotifications
                    .Select(notification => UserNotification.CreateFollowUp(
                        UserNotificationId.New(),
                        followUp,
                        notification,
                        nowUtc))
                    .ToArray();
                await this.notificationCreationService.CreateManyAsync(followUps, cancellationToken);
                await this.digestScheduler.ScheduleAsync(
                    followUp.Id,
                    followUps
                        .Select(static notification => notification.UserId)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    cancellationToken);
            }
            catch (UserNotificationValidationException)
            {
                return DurableBackgroundJobHandlerResult.DeadLetter(
                    FactualNotificationDistributionErrorCodes.InvalidNotification);
            }

            if (followUp.Status == FactualChangeStatus.Retracted)
            {
                return null;
            }

            FactualChangeEvent? refreshed = await this.eventRepository.GetAsync(
                followUp.Id,
                cancellationToken);
            if (refreshed is null)
            {
                return DurableBackgroundJobHandlerResult.DeadLetter(
                    FactualNotificationDistributionErrorCodes.SupersedingEventMissing);
            }

            if (refreshed.Status == FactualChangeStatus.Published)
            {
                return null;
            }

            if (refreshed.Status != FactualChangeStatus.Corrected)
            {
                return refreshed.Status == FactualChangeStatus.Retracted
                    ? null
                    : DurableBackgroundJobHandlerResult.DeadLetter(
                        FactualNotificationDistributionErrorCodes.SupersedingEventMissing);
            }

            followUp = await this.ResolveLatestFollowUpAsync(refreshed, cancellationToken);
            if (followUp is null)
            {
                return DurableBackgroundJobHandlerResult.DeadLetter(
                    FactualNotificationDistributionErrorCodes.SupersedingEventMissing);
            }
        }

        return DurableBackgroundJobHandlerResult.DeadLetter(
            FactualNotificationDistributionErrorCodes.SupersedingEventMissing);
    }

    private async Task<FactualChangeEvent?> ResolveLatestFollowUpAsync(
        FactualChangeEvent original,
        CancellationToken cancellationToken)
    {
        HashSet<FactualChangeEventId> visited = new() { original.Id };
        FactualChangeEvent current = original;
        for (int depth = 0; depth < MaximumCorrectionChainLength; depth++)
        {
            if (!current.SupersededByEventId.HasValue
                || !visited.Add(current.SupersededByEventId.Value))
            {
                return null;
            }

            FactualChangeEvent? candidate = await this.eventRepository.GetAsync(
                current.SupersededByEventId.Value,
                cancellationToken);
            if (candidate is null
                || candidate.PublishedAtUtc is null
                || candidate.Target != original.Target
                || !string.Equals(candidate.DeduplicationKey, original.DeduplicationKey, StringComparison.Ordinal)
                || candidate.Revision <= current.Revision)
            {
                return null;
            }

            if (candidate.Status is FactualChangeStatus.Published or FactualChangeStatus.Retracted)
            {
                return candidate;
            }

            if (candidate.Status != FactualChangeStatus.Corrected)
            {
                return null;
            }

            current = candidate;
        }

        return null;
    }

    private static FactualNotificationCorrectionJobPayload? Parse(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != FactualNotificationCorrectionJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            return context.Payload.Deserialize<FactualNotificationCorrectionJobPayload>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
