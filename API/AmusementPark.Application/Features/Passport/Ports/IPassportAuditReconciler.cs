using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Répare un lot strictement borné de preuves restées attachées à leur source.
/// </summary>
public interface IPassportAuditReconciler
{
    Task<int> ReconcileBatchAsync(
        int maximumEventCount,
        CancellationToken cancellationToken);
}
