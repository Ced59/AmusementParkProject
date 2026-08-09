using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Commands;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkPricing.Handlers;

public sealed class UpsertParkPricingCommandHandler : ICommandHandler<UpsertParkPricingCommand, ApplicationResult<ParkPricing>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkPricingRepository pricingRepository;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public UpsertParkPricingCommandHandler(
        IParkRepository parkRepository,
        IParkPricingRepository pricingRepository,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.parkRepository = parkRepository;
        this.pricingRepository = pricingRepository;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<ParkPricing>> HandleAsync(UpsertParkPricingCommand command, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkPricing> normalizedResult = ParkPricingNormalizer.Normalize(command.Pricing);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            return normalizedResult;
        }

        ParkPricing normalizedPricing = normalizedResult.Value;
        Park? park = await this.parkRepository.GetByIdAsync(normalizedPricing.ParkId, includeHidden: true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkPricing>.Failure(ParkPricingApplicationErrors.ParkNotFound());
        }

        if (!park.Status.IsOpenToVisitors())
        {
            return ApplicationResult<ParkPricing>.Failure(ParkPricingApplicationErrors.PricingNotAllowed(park.Status));
        }

        ParkPricing savedPricing = await this.pricingRepository.UpsertAsync(normalizedPricing, cancellationToken);
        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<ParkPricing>.Success(savedPricing);
    }
}
