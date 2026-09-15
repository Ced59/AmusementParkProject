using AmusementPark.Application.Features.Watchlists.Models;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IFactualNotificationDistributionReceiptRepository
{
    Task<bool> IsCompletedAsync(string eventId, CancellationToken cancellationToken);

    Task CompleteAsync(
        FactualNotificationDistributionReceipt receipt,
        CancellationToken cancellationToken);
}
