using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Services;
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
