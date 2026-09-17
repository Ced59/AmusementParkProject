using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public sealed class NotificationDeliveryAttempt
{
    public const int RetentionDays = 30;

    private NotificationDeliveryAttempt(
        string id,
        string userId,
        NotificationDigestId digestId,
        NotificationDeliveryAttemptStatus status,
        int attemptCount,
        string? lastErrorCode,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? completedAtUtc,
        DateTime expiresAtUtc,
        long version)
    {
        string? normalizedErrorCode = NormalizeOptional(lastErrorCode);
        bool isTerminal = status is NotificationDeliveryAttemptStatus.Succeeded
            or NotificationDeliveryAttemptStatus.Cancelled;
        if (!Enum.IsDefined(status)
            || attemptCount < 0
            || version < 1
            || createdAtUtc.Kind != DateTimeKind.Utc
            || updatedAtUtc.Kind != DateTimeKind.Utc
            || expiresAtUtc.Kind != DateTimeKind.Utc
            || completedAtUtc.HasValue && completedAtUtc.Value.Kind != DateTimeKind.Utc
            || updatedAtUtc < createdAtUtc
            || expiresAtUtc <= createdAtUtc
            || completedAtUtc > updatedAtUtc
            || isTerminal != completedAtUtc.HasValue
            || status == NotificationDeliveryAttemptStatus.Pending && normalizedErrorCode is not null
            || status == NotificationDeliveryAttemptStatus.Failed
                && (attemptCount == 0 || normalizedErrorCode is null)
            || status == NotificationDeliveryAttemptStatus.Succeeded
                && (attemptCount == 0 || normalizedErrorCode is not null)
            || status == NotificationDeliveryAttemptStatus.Cancelled && normalizedErrorCode is null)
        {
            throw new ArgumentException("The notification delivery attempt is invalid.");
        }

        this.Id = IdentifierRules.NormalizeRequired(id, nameof(id));
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        _ = digestId.Value;
        this.DigestId = digestId;
        this.Status = status;
        this.AttemptCount = attemptCount;
        this.LastErrorCode = normalizedErrorCode;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.CompletedAtUtc = completedAtUtc;
        this.ExpiresAtUtc = expiresAtUtc;
        this.Version = version;
    }

    public string Id { get; }

    public string UserId { get; }

    public NotificationDigestId DigestId { get; }

    public NotificationDeliveryAttemptStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public string? LastErrorCode { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public long Version { get; private set; }

    public static NotificationDeliveryAttempt Create(
        string id,
        string userId,
        NotificationDigestId digestId,
        DateTime nowUtc)
    {
        return new NotificationDeliveryAttempt(
            id,
            userId,
            digestId,
            NotificationDeliveryAttemptStatus.Pending,
            0,
            null,
            nowUtc,
            nowUtc,
            null,
            nowUtc.AddDays(RetentionDays),
            1);
    }

    public static NotificationDeliveryAttempt Restore(
        string id,
        string userId,
        NotificationDigestId digestId,
        NotificationDeliveryAttemptStatus status,
        int attemptCount,
        string? lastErrorCode,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? completedAtUtc,
        DateTime expiresAtUtc,
        long version)
    {
        return new NotificationDeliveryAttempt(
            id,
            userId,
            digestId,
            status,
            attemptCount,
            lastErrorCode,
            createdAtUtc,
            updatedAtUtc,
            completedAtUtc,
            expiresAtUtc,
            version);
    }

    public void BeginAttempt(DateTime nowUtc)
    {
        this.EnsureMutable(nowUtc);
        if (this.AttemptCount == int.MaxValue)
        {
            throw new InvalidOperationException("The notification delivery attempt count cannot grow further.");
        }

        this.AttemptCount++;
        this.Status = NotificationDeliveryAttemptStatus.Pending;
        this.LastErrorCode = null;
        this.Commit(nowUtc);
    }

    public void RecordFailure(string errorCode, DateTime nowUtc)
    {
        this.EnsureMutable(nowUtc);
        if (this.AttemptCount == 0)
        {
            throw new InvalidOperationException("A delivery failure requires a started attempt.");
        }

        this.Status = NotificationDeliveryAttemptStatus.Failed;
        this.LastErrorCode = IdentifierRules.NormalizeRequired(errorCode, nameof(errorCode));
        this.Commit(nowUtc);
    }

    public void MarkSucceeded(DateTime nowUtc)
    {
        this.EnsureMutable(nowUtc);
        if (this.AttemptCount == 0)
        {
            throw new InvalidOperationException("A successful delivery requires a started attempt.");
        }

        this.Status = NotificationDeliveryAttemptStatus.Succeeded;
        this.LastErrorCode = null;
        this.CompletedAtUtc = nowUtc;
        this.Commit(nowUtc);
    }

    public void Cancel(string reasonCode, DateTime nowUtc)
    {
        this.EnsureMutable(nowUtc);
        this.Status = NotificationDeliveryAttemptStatus.Cancelled;
        this.LastErrorCode = IdentifierRules.NormalizeRequired(reasonCode, nameof(reasonCode));
        this.CompletedAtUtc = nowUtc;
        this.Commit(nowUtc);
    }

    private void EnsureMutable(DateTime nowUtc)
    {
        if (this.Status is NotificationDeliveryAttemptStatus.Succeeded
            or NotificationDeliveryAttemptStatus.Cancelled)
        {
            throw new InvalidOperationException("A terminal notification delivery attempt cannot be changed.");
        }

        if (nowUtc.Kind != DateTimeKind.Utc || nowUtc < this.UpdatedAtUtc)
        {
            throw new ArgumentException("The notification delivery attempt mutation timestamp is invalid.");
        }

        if (this.Version == long.MaxValue)
        {
            throw new InvalidOperationException("The notification delivery attempt version cannot grow further.");
        }
    }

    private void Commit(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
        this.ExpiresAtUtc = nowUtc.AddDays(RetentionDays);
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }
}
