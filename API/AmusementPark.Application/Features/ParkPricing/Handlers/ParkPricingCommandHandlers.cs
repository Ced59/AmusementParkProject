using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Commands;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkPricing.Handlers;

public sealed class UpsertParkPricingCommandHandler : ICommandHandler<UpsertParkPricingCommand, ApplicationResult<ParkPricingEntity>>
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

    public async Task<ApplicationResult<ParkPricingEntity>> HandleAsync(UpsertParkPricingCommand command, CancellationToken cancellationToken = default)
    {
        if ((command.PreserveHistoricalSnapshots || command.PreserveCreditOffers)
            && !string.IsNullOrWhiteSpace(command.Pricing.ParkId))
        {
            ParkPricingEntity? existingPricing = await this.pricingRepository.GetByParkIdAsync(
                command.Pricing.ParkId.Trim(),
                cancellationToken);
            if (existingPricing is not null)
            {
                if (command.PreserveHistoricalSnapshots)
                {
                    command.Pricing.HistoricalSnapshots = existingPricing.HistoricalSnapshots;
                }

                if (command.PreserveCreditOffers)
                {
                    command.Pricing.CreditOffers = existingPricing.CreditOffers;
                }
            }
        }

        ApplicationResult<ParkPricingEntity> normalizedResult = ParkPricingNormalizer.Normalize(command.Pricing);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            return normalizedResult;
        }

        ParkPricingEntity normalizedPricing = normalizedResult.Value;
        Park? park = await this.parkRepository.GetByIdAsync(normalizedPricing.ParkId, includeHidden: true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.ParkNotFound());
        }

        if (!park.Status.IsOpenToVisitors())
        {
            return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.PricingNotAllowed(park.Status));
        }

        ParkPricingEntity savedPricing = await this.pricingRepository.UpsertAsync(normalizedPricing, cancellationToken);
        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<ParkPricingEntity>.Success(savedPricing);
    }
}
