using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkPricing.Ports;

public interface IParkPricingRepository
{
    Task<ParkPricing?> GetByParkIdAsync(string parkId, CancellationToken cancellationToken);

    Task<ParkPricing> UpsertAsync(ParkPricing pricing, CancellationToken cancellationToken);
}
