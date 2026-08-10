using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Queries;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkPricing.Handlers;

public sealed class GetParkPricingQueryHandler : IQueryHandler<GetParkPricingQuery, ApplicationResult<ParkPricingEntity>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkPricingRepository pricingRepository;
    private readonly TimeProvider timeProvider;

    public GetParkPricingQueryHandler(
        IParkRepository parkRepository,
        IParkPricingRepository pricingRepository,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.pricingRepository = pricingRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ParkPricingEntity>> HandleAsync(GetParkPricingQuery query, CancellationToken cancellationToken = default)
    {
        string parkId = (query.ParkId ?? string.Empty).Trim();
        if (parkId.Length == 0)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.ParkNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(parkId, query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.ParkNotFound());
        }

        if (!query.IncludeHidden && !park.Status.IsOpenToVisitors())
        {
            return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.PricingNotFound());
        }

        ParkPricingEntity? pricing = await this.pricingRepository.GetByParkIdAsync(parkId, cancellationToken);
        if (pricing is null)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.PricingNotFound());
        }

        if (!query.IncludeHidden)
        {
            DateOnly currentDate = DateOnly.FromDateTime(this.timeProvider.GetUtcNow().UtcDateTime);
            pricing = pricing.FilterOffersValidOn(currentDate);
            if (!ParkPricingNormalizer.HasPublicPricingData(pricing))
            {
                return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.PricingNotFound());
            }
        }

        return ApplicationResult<ParkPricingEntity>.Success(pricing);
    }
}
