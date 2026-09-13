using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkPricing.Queries;

public sealed record GetParkPricingQuery(string ParkId, bool IncludeHidden) : IQuery<ApplicationResult<ParkPricingEntity>>;
