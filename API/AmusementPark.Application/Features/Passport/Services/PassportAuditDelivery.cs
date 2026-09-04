using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportAuditDelivery
{
    public static async Task PublishAsync(
        IPassportAuditPublisher? publisher,
        PassportAuditEvent? auditEvent,
        CancellationToken cancellationToken)
    {
        if (publisher is null || auditEvent is null)
        {
            return;
        }

        _ = await publisher.TryPublishAsync(auditEvent, cancellationToken);
    }

    public static async Task PublishAsync(
        IPassportAuditPublisher? publisher,
        IReadOnlyCollection<PassportAuditEvent> auditEvents,
        CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        _ = await publisher.TryPublishBatchAsync(auditEvents, cancellationToken);
    }
}
