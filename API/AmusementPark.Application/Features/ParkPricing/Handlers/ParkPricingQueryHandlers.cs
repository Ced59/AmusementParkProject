using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Queries;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkPricing.Handlers;

public sealed class GetParkPricingQueryHandler : IQueryHandler<GetParkPricingQuery, ApplicationResult<ParkPricing>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkPricingRepository pricingRepository;

    public GetParkPricingQueryHandler(IParkRepository parkRepository, IParkPricingRepository pricingRepository)
    {
        this.parkRepository = parkRepository;
        this.pricingRepository = pricingRepository;
    }

    public async Task<ApplicationResult<ParkPricing>> HandleAsync(GetParkPricingQuery query, CancellationToken cancellationToken = default)
    {
        string parkId = (query.ParkId ?? string.Empty).Trim();
        if (parkId.Length == 0)
        {
            return ApplicationResult<ParkPricing>.Failure(ParkPricingApplicationErrors.ParkNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(parkId, query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkPricing>.Failure(ParkPricingApplicationErrors.ParkNotFound());
        }

        if (!query.IncludeHidden && !park.Status.IsOpenToVisitors())
        {
            return ApplicationResult<ParkPricing>.Failure(ParkPricingApplicationErrors.PricingNotFound());
        }

        ParkPricing? pricing = await this.pricingRepository.GetByParkIdAsync(parkId, cancellationToken);
        if (pricing is null || (!query.IncludeHidden && !ParkPricingNormalizer.HasPublicPricingData(pricing)))
        {
            return ApplicationResult<ParkPricing>.Failure(ParkPricingApplicationErrors.PricingNotFound());
        }

        return ApplicationResult<ParkPricing>.Success(pricing);
    }
}
