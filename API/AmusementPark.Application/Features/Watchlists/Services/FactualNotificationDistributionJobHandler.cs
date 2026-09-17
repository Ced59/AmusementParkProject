using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class FactualNotificationDistributionJobHandler : IDurableBackgroundJobHandler
{
    private readonly IFactualChangeEventRepository eventRepository;
    private readonly IWatchSubscriptionRepository subscriptionRepository;
    private readonly UserNotificationCreationService notificationCreationService;
    private readonly IFactualNotificationDistributionReceiptRepository receiptRepository;
    private readonly IFactualNotificationDistributionScheduler scheduler;
    private readonly INotificationDigestScheduler digestScheduler;
    private readonly IUserRepository userRepository;
    private readonly TimeProvider timeProvider;

    public FactualNotificationDistributionJobHandler(
        IFactualChangeEventRepository eventRepository,
        IWatchSubscriptionRepository subscriptionRepository,
        UserNotificationCreationService notificationCreationService,
        IFactualNotificationDistributionReceiptRepository receiptRepository,
        IFactualNotificationDistributionScheduler scheduler,
        INotificationDigestScheduler digestScheduler,
        IUserRepository userRepository)
        : this(
            eventRepository,
            subscriptionRepository,
            notificationCreationService,
            receiptRepository,
            scheduler,
            digestScheduler,
            userRepository,
            TimeProvider.System)
    {
    }

    internal FactualNotificationDistributionJobHandler(
        IFactualChangeEventRepository eventRepository,
        IWatchSubscriptionRepository subscriptionRepository,
        UserNotificationCreationService notificationCreationService,
        IFactualNotificationDistributionReceiptRepository receiptRepository,
        IFactualNotificationDistributionScheduler scheduler,
        INotificationDigestScheduler digestScheduler,
        IUserRepository userRepository,
        TimeProvider timeProvider)
    {
        this.eventRepository = eventRepository;
        this.subscriptionRepository = subscriptionRepository;
        this.notificationCreationService = notificationCreationService
            ?? throw new ArgumentNullException(nameof(notificationCreationService));
        this.receiptRepository = receiptRepository;
        this.scheduler = scheduler;
        this.digestScheduler = digestScheduler ?? throw new ArgumentNullException(nameof(digestScheduler));
        this.userRepository = userRepository;
        this.timeProvider = timeProvider;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            FactualNotificationDistributionJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { FactualNotificationDistributionJob.PayloadVersion },
            TimeSpan.FromMinutes(2),
            maximumAttempts: 5,
            initialRetryDelay: TimeSpan.FromSeconds(30),
            maximumRetryDelay: TimeSpan.FromMinutes(10),
            maximumConcurrency: 2);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        FactualNotificationDistributionJobPayload? payload = Parse(context);
        if (payload is null || !FactualChangeEventId.TryParse(payload.EventId, out FactualChangeEventId eventId))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.InvalidPayload);
        }

        if (await this.receiptRepository.IsCompletedAsync(eventId.Value, cancellationToken))
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        FactualChangeEvent? factualEvent = await this.eventRepository.GetAsync(eventId, cancellationToken);
        if (factualEvent is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.EventMissing);
        }

        if (!factualEvent.CanBeDistributed)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.EventNotPublished);
        }

        IReadOnlyCollection<WatchSubscription> subscriptions = await this.subscriptionRepository.ListMatchingAsync(
            factualEvent,
            payload.AfterSubscriptionId,
            FactualNotificationDistributionJob.SubscriptionBatchSize,
            cancellationToken);
        string[] userIds = subscriptions
            .Select(static subscription => subscription.UserId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<User> users = await this.userRepository.GetByIdsAsync(userIds, cancellationToken);
        Dictionary<string, User> usersById = users.ToDictionary(static user => user.Id, StringComparer.Ordinal);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        List<UserNotification> notifications = new();
        try
        {
            foreach (WatchSubscription subscription in subscriptions)
            {
                if (!usersById.TryGetValue(subscription.UserId, out User? user))
                {
                    continue;
                }

                string language = PreferredLanguagePolicy.TryNormalize(user.PreferredLanguage, out string normalizedLanguage)
                    ? normalizedLanguage
                    : "EN";
                notifications.Add(UserNotification.CreateWeb(
                    UserNotificationId.New(),
                    factualEvent,
                    subscription,
                    language,
                    nowUtc));
            }
        }
        catch (UserNotificationValidationException)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                FactualNotificationDistributionErrorCodes.InvalidNotification);
        }

        await this.notificationCreationService.CreateManyAsync(notifications, cancellationToken);
        await this.digestScheduler.ScheduleAsync(
            factualEvent.Id,
            notifications
                .Select(static notification => notification.UserId)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            cancellationToken);
        if (subscriptions.Count == FactualNotificationDistributionJob.SubscriptionBatchSize)
        {
            await this.scheduler.ScheduleAsync(
                eventId.Value,
                subscriptions.Last().Id.Value,
                cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }

        await this.receiptRepository.CompleteAsync(
            new FactualNotificationDistributionReceipt(eventId.Value, nowUtc),
            cancellationToken);
        return DurableBackgroundJobHandlerResult.Success();
    }

    private static FactualNotificationDistributionJobPayload? Parse(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != FactualNotificationDistributionJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            FactualNotificationDistributionJobPayload? payload =
                context.Payload.Deserialize<FactualNotificationDistributionJobPayload>();
            return payload is not null && !string.IsNullOrWhiteSpace(payload.EventId)
                ? payload
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
