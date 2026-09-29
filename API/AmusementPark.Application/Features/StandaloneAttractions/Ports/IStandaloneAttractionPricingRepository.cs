using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.StandaloneAttractions.Ports;

public interface IStandaloneAttractionPricingRepository
{
    Task<ParkPricingEntity?> GetByStandaloneAttractionIdAsync(
        string standaloneAttractionId,
        CancellationToken cancellationToken);

    Task<ParkPricingEntity> UpsertAsync(
        ParkPricingEntity pricing,
        CancellationToken cancellationToken);
}
