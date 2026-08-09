using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkPricing.Queries;

public sealed record GetParkPricingQuery(string ParkId, bool IncludeHidden) : IQuery<ApplicationResult<ParkPricing>>;
