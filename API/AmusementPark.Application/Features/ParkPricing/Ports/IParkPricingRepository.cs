using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkPricing.Ports;

public interface IParkPricingRepository
{
    Task<ParkPricingEntity?> GetByParkIdAsync(string parkId, CancellationToken cancellationToken);

    Task<ParkPricingEntity> UpsertAsync(ParkPricingEntity pricing, CancellationToken cancellationToken);
}
