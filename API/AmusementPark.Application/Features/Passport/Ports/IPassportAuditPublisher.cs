using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Publie de façon idempotente une preuve déjà attachée à sa source durable.
/// Un échec de publication ne remet jamais en cause la mutation métier validée.
/// </summary>
public interface IPassportAuditPublisher
{
    Task<bool> TryPublishAsync(
        PassportAuditEvent auditEvent,
        CancellationToken cancellationToken);

    async Task<bool> TryPublishBatchAsync(
        IReadOnlyCollection<PassportAuditEvent> auditEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvents);
        foreach (PassportAuditEvent auditEvent in auditEvents)
        {
            if (!await this.TryPublishAsync(auditEvent, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Répare un lot strictement borné de preuves restées attachées à leur source.
/// </summary>
public interface IPassportAuditReconciler
{
    Task<int> ReconcileBatchAsync(
        int maximumEventCount,
        CancellationToken cancellationToken);
}
