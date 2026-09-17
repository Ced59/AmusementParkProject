using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// Private Web delivery of one published factual event to one member.
/// </summary>
public sealed class UserNotification
{
    public const int CurrentTemplateVersion = 1;
    public const int RetentionDays = 365;

    private UserNotification(
        UserNotificationId id,
        string userId,
        FactualChangeEventId factualEventId,
        WatchSubscriptionId subscriptionId,
        FactualEventType eventType,
        FactualTargetType targetType,
        string targetId,
        string parkId,
        long sourceRevision,
        int templateVersion,
        string language,
        UserNotificationStatus status,
        DateTime createdAtUtc,
        DateTime deliveredAtUtc,
        DateTime? readAtUtc,
        DateTime? dismissedAtUtc,
        DateTime expiresAtUtc,
        long version,
        DateTime? misleadingReportedAtUtc)
    {
        _ = id.Value;
        _ = factualEventId.Value;
        _ = subscriptionId.Value;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        if (!Enum.IsDefined(eventType) || !Enum.IsDefined(targetType))
        {
            throw Invalid(UserNotificationErrorCodes.SubscriptionDoesNotMatch, "The notification fact is invalid.");
        }

        this.TargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        if (sourceRevision < 1 || templateVersion < 1)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidVersion, "Notification revisions must be positive.");
        }

        if (!PreferredLanguagePolicy.TryNormalize(language, out string normalizedLanguage))
        {
            throw Invalid(UserNotificationErrorCodes.InvalidLanguage, "The notification language is not supported.");
        }

        ValidateState(
            status,
            createdAtUtc,
            deliveredAtUtc,
            readAtUtc,
            dismissedAtUtc,
            expiresAtUtc,
            version,
            misleadingReportedAtUtc);
        this.Id = id;
        this.FactualEventId = factualEventId;
        this.SubscriptionId = subscriptionId;
        this.EventType = eventType;
        this.TargetType = targetType;
        this.SourceRevision = sourceRevision;
        this.TemplateVersion = templateVersion;
        this.Language = normalizedLanguage;
        this.Status = status;
        this.CreatedAtUtc = createdAtUtc;
        this.DeliveredAtUtc = deliveredAtUtc;
        this.ReadAtUtc = readAtUtc;
        this.DismissedAtUtc = dismissedAtUtc;
        this.ExpiresAtUtc = expiresAtUtc;
        this.Version = version;
        this.MisleadingReportedAtUtc = misleadingReportedAtUtc;
    }

    public UserNotificationId Id { get; }

    public string UserId { get; }

    public FactualChangeEventId FactualEventId { get; }

    public WatchSubscriptionId SubscriptionId { get; }

    public FactualEventType EventType { get; }

    public FactualTargetType TargetType { get; }

    public string TargetId { get; }

    public string ParkId { get; }

    public long SourceRevision { get; }

    public int TemplateVersion { get; }

    public string Language { get; }

    public UserNotificationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime DeliveredAtUtc { get; }

    public DateTime? ReadAtUtc { get; private set; }

    public DateTime? DismissedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; }

    public long Version { get; private set; }

    public DateTime? MisleadingReportedAtUtc { get; private set; }

    public bool IsUnread => this.Status == UserNotificationStatus.Delivered;

    public static UserNotification CreateWeb(
        UserNotificationId id,
        FactualChangeEvent factualEvent,
        WatchSubscription subscription,
        string language,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(factualEvent);
        ArgumentNullException.ThrowIfNull(subscription);
        if (!factualEvent.CanBeDistributed)
        {
            throw Invalid(UserNotificationErrorCodes.EventNotDistributable, "Only published facts can be delivered.");
        }

        if (!subscription.Accepts(factualEvent))
        {
            throw Invalid(UserNotificationErrorCodes.SubscriptionDoesNotMatch, "The subscription does not accept this fact.");
        }

        string parkId = factualEvent.Target.Type == FactualTargetType.Park
            ? factualEvent.Target.TargetId
            : factualEvent.Target.ParentParkId
                ?? throw Invalid(UserNotificationErrorCodes.SubscriptionDoesNotMatch, "A park item fact must identify its park.");
        EnsureUtc(nowUtc);
        return new UserNotification(
            id,
            subscription.UserId,
            factualEvent.Id,
            subscription.Id,
            factualEvent.Type,
            factualEvent.Target.Type,
            factualEvent.Target.TargetId,
            parkId,
            factualEvent.Revision,
            CurrentTemplateVersion,
            language,
            UserNotificationStatus.Delivered,
            nowUtc,
            nowUtc,
            null,
            null,
            nowUtc.AddDays(RetentionDays),
            1,
            null);
    }

    public static UserNotification CreateFollowUp(
        UserNotificationId id,
        FactualChangeEvent followUp,
        UserNotification originalNotification,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(followUp);
        ArgumentNullException.ThrowIfNull(originalNotification);
        if (followUp.PublishedAtUtc is null
            || followUp.Status is not FactualChangeStatus.Published
                and not FactualChangeStatus.Retracted)
        {
            throw Invalid(
                UserNotificationErrorCodes.EventNotDistributable,
                "A follow-up must reference a published or retracted factual event.");
        }

        if (followUp.Target.Type != originalNotification.TargetType
            || !string.Equals(followUp.Target.TargetId, originalNotification.TargetId, StringComparison.Ordinal))
        {
            throw Invalid(
                UserNotificationErrorCodes.SubscriptionDoesNotMatch,
                "A correction must keep the original notification target.");
        }

        EnsureUtc(nowUtc);
        return new UserNotification(
            id,
            originalNotification.UserId,
            followUp.Id,
            originalNotification.SubscriptionId,
            followUp.Type,
            followUp.Target.Type,
            followUp.Target.TargetId,
            originalNotification.ParkId,
            followUp.Revision,
            CurrentTemplateVersion,
            originalNotification.Language,
            UserNotificationStatus.Delivered,
            nowUtc,
            nowUtc,
            null,
            null,
            nowUtc.AddDays(RetentionDays),
            1,
            null);
    }

    public static UserNotification Restore(
        UserNotificationId id,
        string userId,
        FactualChangeEventId factualEventId,
        WatchSubscriptionId subscriptionId,
        FactualEventType eventType,
        FactualTargetType targetType,
        string targetId,
        string parkId,
        long sourceRevision,
        int templateVersion,
        string language,
        UserNotificationStatus status,
        DateTime createdAtUtc,
        DateTime deliveredAtUtc,
        DateTime? readAtUtc,
        DateTime? dismissedAtUtc,
        DateTime expiresAtUtc,
        long version,
        DateTime? misleadingReportedAtUtc = null)
    {
        return new UserNotification(
            id,
            userId,
            factualEventId,
            subscriptionId,
            eventType,
            targetType,
            targetId,
            parkId,
            sourceRevision,
            templateVersion,
            language,
            status,
            createdAtUtc,
            deliveredAtUtc,
            readAtUtc,
            dismissedAtUtc,
            expiresAtUtc,
            version,
            misleadingReportedAtUtc);
    }

    public void MarkRead(DateTime nowUtc)
    {
        this.EnsureMutationTimestamp(nowUtc);
        if (this.Status == UserNotificationStatus.Read)
        {
            return;
        }

        if (this.Status != UserNotificationStatus.Delivered)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidTransition, "Only a delivered notification can be marked as read.");
        }

        this.AdvanceVersion();
        this.Status = UserNotificationStatus.Read;
        this.ReadAtUtc = nowUtc;
    }

    public void Dismiss(DateTime nowUtc)
    {
        this.EnsureMutationTimestamp(nowUtc);
        if (this.Status == UserNotificationStatus.Dismissed)
        {
            return;
        }

        if (this.Status is not UserNotificationStatus.Delivered and not UserNotificationStatus.Read)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidTransition, "Only a delivered notification can be dismissed.");
        }

        this.AdvanceVersion();
        this.Status = UserNotificationStatus.Dismissed;
        this.DismissedAtUtc = nowUtc;
    }

    public void ReportMisleading(DateTime nowUtc)
    {
        this.EnsureMutationTimestamp(nowUtc);
        if (this.MisleadingReportedAtUtc.HasValue)
        {
            return;
        }

        this.AdvanceVersion();
        this.MisleadingReportedAtUtc = nowUtc;
    }

    private static void ValidateState(
        UserNotificationStatus status,
        DateTime createdAtUtc,
        DateTime deliveredAtUtc,
        DateTime? readAtUtc,
        DateTime? dismissedAtUtc,
        DateTime expiresAtUtc,
        long version,
        DateTime? misleadingReportedAtUtc)
    {
        if (!Enum.IsDefined(status))
        {
            throw Invalid(UserNotificationErrorCodes.InvalidStatus, "The notification status is invalid.");
        }

        EnsureUtc(createdAtUtc);
        EnsureUtc(deliveredAtUtc);
        EnsureUtc(expiresAtUtc);
        if (readAtUtc.HasValue)
        {
            EnsureUtc(readAtUtc.Value);
        }

        if (dismissedAtUtc.HasValue)
        {
            EnsureUtc(dismissedAtUtc.Value);
        }

        if (misleadingReportedAtUtc.HasValue)
        {
            EnsureUtc(misleadingReportedAtUtc.Value);
        }

        if (deliveredAtUtc < createdAtUtc || expiresAtUtc <= deliveredAtUtc
            || readAtUtc < deliveredAtUtc || dismissedAtUtc < deliveredAtUtc
            || misleadingReportedAtUtc < deliveredAtUtc)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidTimestamp, "The notification timestamps are inconsistent.");
        }

        if ((status == UserNotificationStatus.Delivered && (readAtUtc.HasValue || dismissedAtUtc.HasValue))
            || (status == UserNotificationStatus.Read && (!readAtUtc.HasValue || dismissedAtUtc.HasValue))
            || (status == UserNotificationStatus.Dismissed && !dismissedAtUtc.HasValue))
        {
            throw Invalid(UserNotificationErrorCodes.InvalidStatus, "The notification status does not match its timestamps.");
        }

        if (version < 1)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidVersion, "The notification version must be positive.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidTimestamp, "Notification timestamps must use UTC.");
        }
    }

    private void EnsureMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.DeliveredAtUtc)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidTimestamp, "A notification mutation cannot predate delivery.");
        }
    }

    private void AdvanceVersion()
    {
        if (this.Version == long.MaxValue)
        {
            throw Invalid(UserNotificationErrorCodes.InvalidVersion, "The notification version cannot be incremented further.");
        }

        this.Version++;
    }

    private static UserNotificationValidationException Invalid(string code, string message)
    {
        return new UserNotificationValidationException(code, message);
    }
}
