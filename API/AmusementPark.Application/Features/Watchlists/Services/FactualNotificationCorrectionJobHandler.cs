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
    private readonly IFactualChangeEventRepository eventRepository;
    private readonly IUserNotificationRepository notificationRepository;
    private readonly IFactualNotificationDistributionReceiptRepository receiptRepository;
    private readonly IFactualNotificationDistributionScheduler scheduler;
    private readonly TimeProvider timeProvider;

    public FactualNotificationCorrectionJobHandler(
        IFactualChangeEventRepository eventRepository,
        IUserNotificationRepository notificationRepository,
        IFactualNotificationDistributionReceiptRepository receiptRepository,
        IFactualNotificationDistributionScheduler scheduler)
        : this(
            eventRepository,
            notificationRepository,
            receiptRepository,
            scheduler,
            TimeProvider.System)
    {
    }

    internal FactualNotificationCorrectionJobHandler(
        IFactualChangeEventRepository eventRepository,
        IUserNotificationRepository notificationRepository,
        IFactualNotificationDistributionReceiptRepository receiptRepository,
        IFactualNotificationDistributionScheduler scheduler,
        TimeProvider timeProvider)
    {
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        this.receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
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
        if (!original.SupersededByEventId.HasValue)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.SupersedingEventMissing);
        }

        FactualChangeEvent? correction = await this.eventRepository.GetAsync(
            original.SupersededByEventId.Value,
            cancellationToken);
        if (correction?.Status != FactualChangeStatus.Published
            || correction.PublishedAtUtc is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.SupersedingEventMissing);
        }

        try
        {
            UserNotification[] corrections = originalNotifications
                .Select(notification => UserNotification.CreateCorrection(
                    UserNotificationId.New(),
                    correction,
                    notification,
                    nowUtc))
                .ToArray();
            await this.notificationRepository.CreateManyAsync(corrections, cancellationToken);
            return null;
        }
        catch (UserNotificationValidationException)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.InvalidNotification);
        }
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
