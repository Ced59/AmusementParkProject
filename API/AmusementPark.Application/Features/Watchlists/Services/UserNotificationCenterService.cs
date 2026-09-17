using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class UserNotificationCenterService
{
    private readonly IUserNotificationRepository notificationRepository;
    private readonly IWatchSubscriptionRepository subscriptionRepository;
    private readonly IFactualChangeEventRepository eventRepository;
    private readonly UserCollectionTargetReader targetReader;
    private readonly TimeProvider timeProvider;

    public UserNotificationCenterService(
        IUserNotificationRepository notificationRepository,
        IWatchSubscriptionRepository subscriptionRepository,
        IFactualChangeEventRepository eventRepository,
        UserCollectionTargetReader targetReader,
        TimeProvider? timeProvider = null)
    {
        this.notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        this.subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.targetReader = targetReader ?? throw new ArgumentNullException(nameof(targetReader));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<UserNotificationPageResult>> SearchAsync(
        string userId,
        UserNotificationSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        string normalizedUserId;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return ApplicationResult<UserNotificationPageResult>.Failure(
                UserNotificationApplicationErrors.Invalid());
        }

        long skip = ((long)criteria.Paging.Page - 1L) * criteria.Paging.PageSize;
        if (criteria.Paging.Page < 1 || criteria.Paging.PageSize is < 1 or > 50
            || skip > int.MaxValue
            || criteria.EventType.HasValue && !Enum.IsDefined(criteria.EventType.Value))
        {
            return ApplicationResult<UserNotificationPageResult>.Failure(
                UserNotificationApplicationErrors.Invalid());
        }

        PagedResult<UserNotification> page = await this.notificationRepository.SearchOwnedAsync(
            normalizedUserId,
            criteria,
            cancellationToken);
        FactualChangeEventId[] eventIds = page.Items
            .Select(static notification => notification.FactualEventId)
            .Distinct()
            .ToArray();
        IReadOnlyCollection<FactualChangeEvent> events = await this.eventRepository.GetManyAsync(
            eventIds,
            cancellationToken);
        Dictionary<FactualChangeEventId, FactualChangeEvent> eventsById = events.ToDictionary(
            static factualEvent => factualEvent.Id);
        IReadOnlyCollection<FactualChangeEvent> correctedOriginals =
            await this.eventRepository.GetCorrectedBySuccessorIdsAsync(eventIds, cancellationToken);
        IReadOnlyCollection<FactualChangeEventId> deliveredOriginalIds =
            await this.notificationRepository.ListDeliveredFactualEventIdsOwnedAsync(
                normalizedUserId,
                correctedOriginals.Select(static factualEvent => factualEvent.Id).ToArray(),
                cancellationToken);
        HashSet<FactualChangeEventId> deliveredOriginalIdSet = deliveredOriginalIds.ToHashSet();
        Dictionary<FactualChangeEventId, FactualChangeEvent> correctedOriginalsBySuccessor = correctedOriginals
            .Where(factualEvent => factualEvent.SupersededByEventId.HasValue
                && deliveredOriginalIdSet.Contains(factualEvent.Id))
            .GroupBy(static factualEvent => factualEvent.SupersededByEventId!.Value)
            .ToDictionary(
                static group => group.Key,
                static group => group.OrderByDescending(static factualEvent => factualEvent.TerminalAtUtc).First());
        WatchSubscriptionId[] subscriptionIds = page.Items
            .Select(static notification => notification.SubscriptionId)
            .Distinct()
            .ToArray();
        IReadOnlyCollection<WatchSubscription> subscriptions = await this.subscriptionRepository.ListOwnedByIdsAsync(
            normalizedUserId,
            subscriptionIds,
            cancellationToken);
        Dictionary<WatchSubscriptionId, WatchSubscription> subscriptionsById = subscriptions.ToDictionary(
            static subscription => subscription.Id);
        IReadOnlyDictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> targets =
            await this.ResolveTargetsAsync(events, cancellationToken);

        UserNotificationResult[] results = page.Items
            .Where(notification => eventsById.ContainsKey(notification.FactualEventId))
            .Select(notification => Map(
                notification,
                eventsById[notification.FactualEventId],
                FindTarget(targets, eventsById[notification.FactualEventId]),
                subscriptionsById.GetValueOrDefault(notification.SubscriptionId),
                correctedOriginalsBySuccessor.GetValueOrDefault(notification.FactualEventId)))
            .ToArray();
        long unreadCount = await this.notificationRepository.CountUnreadAsync(
            normalizedUserId,
            cancellationToken);
        IReadOnlyCollection<UserNotificationParkFilterResult> parkFilters =
            await this.ResolveParkFiltersAsync(normalizedUserId, cancellationToken);
        return ApplicationResult<UserNotificationPageResult>.Success(
            new UserNotificationPageResult(
                results,
                page.Page,
                page.PageSize,
                page.TotalItems,
                unreadCount,
                UserNotification.RetentionDays,
                parkFilters));
    }

    public Task<ApplicationResult> MarkReadAsync(
        string userId,
        string notificationId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.MutateAsync(
            userId,
            notificationId,
            expectedVersion,
            static (notification, nowUtc) => notification.MarkRead(nowUtc),
            cancellationToken);
    }

    public Task<ApplicationResult> DismissAsync(
        string userId,
        string notificationId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.MutateAsync(
            userId,
            notificationId,
            expectedVersion,
            static (notification, nowUtc) => notification.Dismiss(nowUtc),
            cancellationToken);
    }

    public async Task<ApplicationResult> MarkAllReadAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            await this.notificationRepository.MarkAllReadAsync(
                normalizedUserId,
                this.timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);
            return ApplicationResult.Success();
        }
        catch (ArgumentException)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.Invalid());
        }
    }

    public async Task<ApplicationResult> DeleteSourceSubscriptionAsync(
        string userId,
        string notificationId,
        long expectedSubscriptionVersion,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(userId, notificationId, out string normalizedUserId, out UserNotificationId parsedId)
            || expectedSubscriptionVersion < 1)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.Invalid());
        }

        UserNotification? notification = await this.notificationRepository.GetOwnedAsync(
            normalizedUserId,
            parsedId,
            cancellationToken);
        if (notification is null)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.NotFound());
        }

        WatchSubscription? subscription = await this.subscriptionRepository.GetOwnedAsync(
            normalizedUserId,
            notification.SubscriptionId,
            cancellationToken);
        if (subscription is null)
        {
            return ApplicationResult.Success();
        }

        if (subscription.Version != expectedSubscriptionVersion)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.ChangedConcurrently());
        }

        WatchSubscriptionWriteOutcome outcome = await this.subscriptionRepository.DeleteAsync(
            normalizedUserId,
            subscription.Id,
            expectedSubscriptionVersion,
            cancellationToken);
        return outcome is WatchSubscriptionWriteOutcome.Success or WatchSubscriptionWriteOutcome.NotFound
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(UserNotificationApplicationErrors.ChangedConcurrently());
    }

    private async Task<ApplicationResult> MutateAsync(
        string userId,
        string notificationId,
        long expectedVersion,
        Action<UserNotification, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(userId, notificationId, out string normalizedUserId, out UserNotificationId parsedId)
            || expectedVersion < 1)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.Invalid());
        }

        UserNotification? notification = await this.notificationRepository.GetOwnedAsync(
            normalizedUserId,
            parsedId,
            cancellationToken);
        if (notification is null)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.NotFound());
        }

        if (notification.Version != expectedVersion)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.ChangedConcurrently());
        }

        try
        {
            mutation(notification, this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (UserNotificationValidationException)
        {
            return ApplicationResult.Failure(UserNotificationApplicationErrors.Invalid());
        }

        if (notification.Version == expectedVersion)
        {
            return ApplicationResult.Success();
        }

        UserNotificationWriteOutcome outcome = await this.notificationRepository.ReplaceAsync(
            notification,
            expectedVersion,
            cancellationToken);
        return outcome == UserNotificationWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(UserNotificationApplicationErrors.ChangedConcurrently());
    }

    private async Task<IReadOnlyDictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>>>
        ResolveTargetsAsync(
            IReadOnlyCollection<FactualChangeEvent> events,
            CancellationToken cancellationToken)
    {
        Dictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> result = new();
        foreach (IGrouping<FactualTargetType, FactualChangeEvent> group in events.GroupBy(
            static factualEvent => factualEvent.Target.Type))
        {
            CollectionTargetType targetType = group.Key == FactualTargetType.Park
                ? CollectionTargetType.Park
                : CollectionTargetType.ParkItem;
            result[targetType] = await this.targetReader.ResolveAsync(
                targetType,
                group.Select(static factualEvent => factualEvent.Target.TargetId).ToArray(),
                cancellationToken);
        }

        return result;
    }

    private async Task<IReadOnlyCollection<UserNotificationParkFilterResult>> ResolveParkFiltersAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> parkIds = await this.notificationRepository.ListParkIdsOwnedAsync(
            userId,
            cancellationToken);
        IReadOnlyDictionary<string, UserCollectionTargetSnapshot> parks = await this.targetReader.ResolveAsync(
            CollectionTargetType.Park,
            parkIds,
            cancellationToken);
        return parks.Values
            .Where(static park => !string.IsNullOrWhiteSpace(park.Name))
            .Select(static park => new UserNotificationParkFilterResult(park.TargetId, park.Name!))
            .OrderBy(static park => park.ParkName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static UserCollectionTargetSnapshot FindTarget(
        IReadOnlyDictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> targets,
        FactualChangeEvent factualEvent)
    {
        CollectionTargetType targetType = factualEvent.Target.Type == FactualTargetType.Park
            ? CollectionTargetType.Park
            : CollectionTargetType.ParkItem;
        return targets.TryGetValue(targetType, out IReadOnlyDictionary<string, UserCollectionTargetSnapshot>? byId)
            && byId.TryGetValue(factualEvent.Target.TargetId, out UserCollectionTargetSnapshot? snapshot)
                ? snapshot
                : new UserCollectionTargetSnapshot(
                    targetType,
                    factualEvent.Target.TargetId,
                    CollectionTargetStatus.Unknown,
                    null,
                    null,
                    null,
                    null);
    }

    private static UserNotificationResult Map(
        UserNotification notification,
        FactualChangeEvent factualEvent,
        UserCollectionTargetSnapshot target,
        WatchSubscription? subscription,
        FactualChangeEvent? correctedOriginal)
    {
        DateTime verifiedAtUtc = factualEvent.VerifiedAtUtc
            ?? throw new InvalidOperationException("A delivered factual event must have been verified.");
        return new UserNotificationResult(
            notification.Id.Value,
            factualEvent.Type,
            notification.Status,
            ResolveNoticeKind(factualEvent, correctedOriginal),
            factualEvent.Status,
            correctedOriginal?.TerminalAtUtc ?? factualEvent.TerminalAtUtc,
            factualEvent.Status == FactualChangeStatus.Retracted ? factualEvent.ReasonCode : null,
            new UserNotificationTargetResult(
                factualEvent.Target.Type,
                factualEvent.Target.TargetId,
                factualEvent.Target.Type == FactualTargetType.Park
                    ? factualEvent.Target.TargetId
                    : target.ParentParkId,
                target.Name,
                target.ParentParkName,
                target.MainImageId),
            Map(factualEvent.PreviousValue),
            Map(factualEvent.NewValue),
            new UserNotificationSourceResult(
                factualEvent.Source.Type,
                factualEvent.Source.PublisherName,
                factualEvent.Source.Title,
                factualEvent.Source.Url,
                factualEvent.Source.PublishedAtUtc,
                verifiedAtUtc),
            factualEvent.OccurredAtUtc,
            notification.DeliveredAtUtc,
            notification.ReadAtUtc,
            notification.ExpiresAtUtc,
            notification.Version,
            subscription is not null,
            subscription?.Version);
    }

    private static UserNotificationNoticeKind ResolveNoticeKind(
        FactualChangeEvent factualEvent,
        FactualChangeEvent? correctedOriginal)
    {
        return factualEvent.Status switch
        {
            FactualChangeStatus.Corrected => UserNotificationNoticeKind.Superseded,
            FactualChangeStatus.Retracted => UserNotificationNoticeKind.Retraction,
            _ when correctedOriginal is not null => UserNotificationNoticeKind.Correction,
            _ => UserNotificationNoticeKind.Update,
        };
    }

    private static UserNotificationFactValueResult? Map(FactValue? value)
    {
        return value is null
            ? null
            : new UserNotificationFactValueResult(value.Kind, value.CanonicalValue, value.UnitCode);
    }

    private static bool TryNormalize(
        string userId,
        string notificationId,
        out string normalizedUserId,
        out UserNotificationId parsedId)
    {
        normalizedUserId = string.Empty;
        parsedId = default;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return false;
        }

        return UserNotificationId.TryParse(notificationId, out parsedId);
    }
}
