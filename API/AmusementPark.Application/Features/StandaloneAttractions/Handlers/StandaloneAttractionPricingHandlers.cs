using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class GetStandaloneAttractionPricingQueryHandler :
    IQueryHandler<GetStandaloneAttractionPricingQuery, ApplicationResult<ParkPricingEntity>>
{
    private readonly IStandaloneAttractionRepository attractionRepository;
    private readonly IStandaloneAttractionPricingRepository pricingRepository;
    private readonly TimeProvider timeProvider;

    public GetStandaloneAttractionPricingQueryHandler(
        IStandaloneAttractionRepository attractionRepository,
        IStandaloneAttractionPricingRepository pricingRepository,
        TimeProvider? timeProvider = null)
    {
        this.attractionRepository = attractionRepository;
        this.pricingRepository = pricingRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ParkPricingEntity>> HandleAsync(
        GetStandaloneAttractionPricingQuery query,
        CancellationToken cancellationToken = default)
    {
        string attractionId = (query.StandaloneAttractionId ?? string.Empty).Trim();
        StandaloneAttraction? attraction = attractionId.Length == 0
            ? null
            : await this.attractionRepository.GetByIdAsync(
                attractionId,
                query.IncludeHidden,
                cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        if (!query.IncludeHidden
            && !ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
        {
            return ApplicationResult<ParkPricingEntity>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.PricingNotFound());
        }

        ParkPricingEntity? pricing =
            await this.pricingRepository.GetByStandaloneAttractionIdAsync(
                attractionId,
                cancellationToken);
        if (pricing is null)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.PricingNotFound());
        }

        if (!query.IncludeHidden)
        {
            DateOnly currentDate = DateOnly.FromDateTime(this.timeProvider.GetUtcNow().UtcDateTime);
            pricing = pricing.FilterOffersValidOn(currentDate);
            if (!ParkPricingNormalizer.HasPublicPricingData(pricing))
            {
                return ApplicationResult<ParkPricingEntity>.Failure(
                    StandaloneAttractionVisitorInformationApplicationErrors.PricingNotFound());
            }
        }

        return ApplicationResult<ParkPricingEntity>.Success(pricing);
    }
}

public sealed class UpsertStandaloneAttractionPricingCommandHandler :
    ICommandHandler<UpsertStandaloneAttractionPricingCommand, ApplicationResult<ParkPricingEntity>>
{
    private readonly IStandaloneAttractionRepository attractionRepository;
    private readonly IStandaloneAttractionPricingRepository pricingRepository;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public UpsertStandaloneAttractionPricingCommandHandler(
        IStandaloneAttractionRepository attractionRepository,
        IStandaloneAttractionPricingRepository pricingRepository,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.attractionRepository = attractionRepository;
        this.pricingRepository = pricingRepository;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<ParkPricingEntity>> HandleAsync(
        UpsertStandaloneAttractionPricingCommand command,
        CancellationToken cancellationToken = default)
    {
        if ((command.PreserveHistoricalSnapshots || command.PreserveCreditOffers)
            && !string.IsNullOrWhiteSpace(command.Pricing.ParkId))
        {
            ParkPricingEntity? existing =
                await this.pricingRepository.GetByStandaloneAttractionIdAsync(
                    command.Pricing.ParkId.Trim(),
                    cancellationToken);
            if (existing is not null)
            {
                if (command.PreserveHistoricalSnapshots)
                {
                    command.Pricing.HistoricalSnapshots = existing.HistoricalSnapshots;
                }

                if (command.PreserveCreditOffers)
                {
                    command.Pricing.CreditOffers = existing.CreditOffers;
                }
            }
        }

        ApplicationResult<ParkPricingEntity> normalizedResult =
            ParkPricingNormalizer.Normalize(command.Pricing);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            return normalizedResult;
        }

        ParkPricingEntity pricing = normalizedResult.Value;
        StandaloneAttraction? attraction = await this.attractionRepository.GetByIdAsync(
            pricing.ParkId,
            includeHidden: true,
            cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        if (!ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
        {
            return ApplicationResult<ParkPricingEntity>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.CurrentInformationNotAllowed());
        }

        ParkPricingEntity saved = await this.pricingRepository.UpsertAsync(pricing, cancellationToken);
        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<ParkPricingEntity>.Success(saved);
    }
}
