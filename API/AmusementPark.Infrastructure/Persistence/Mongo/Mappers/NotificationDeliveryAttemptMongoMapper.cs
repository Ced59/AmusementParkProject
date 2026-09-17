using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class NotificationDeliveryAttemptMongoMapper
{
    public static NotificationDeliveryAttemptDocument ToDocument(
        this NotificationDeliveryAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        return new NotificationDeliveryAttemptDocument
        {
            Id = attempt.Id,
            UserId = attempt.UserId,
            DigestId = attempt.DigestId.Value,
            Status = attempt.Status,
            AttemptCount = attempt.AttemptCount,
            LastErrorCode = attempt.LastErrorCode,
            CreatedAt = attempt.CreatedAtUtc,
            UpdatedAt = attempt.UpdatedAtUtc,
            CompletedAt = attempt.CompletedAtUtc,
            ExpiresAt = attempt.ExpiresAtUtc,
            Version = attempt.Version,
        };
    }

    public static NotificationDeliveryAttempt ToDomain(
        this NotificationDeliveryAttemptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return NotificationDeliveryAttempt.Restore(
            document.Id,
            document.UserId,
            NotificationDigestId.Parse(document.DigestId),
            document.Status,
            document.AttemptCount,
            document.LastErrorCode,
            document.CreatedAt,
            document.UpdatedAt,
            document.CompletedAt,
            document.ExpiresAt,
            document.Version);
    }
}
