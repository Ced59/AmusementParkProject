using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.StandaloneAttractions.Queries;

public sealed record GetStandaloneAttractionPricingQuery(
    string StandaloneAttractionId,
    bool IncludeHidden) : IQuery<ApplicationResult<ParkPricingEntity>>;
