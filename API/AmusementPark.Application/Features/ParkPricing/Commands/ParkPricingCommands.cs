using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkPricing.Commands;

public sealed record UpsertParkPricingCommand(ParkPricing Pricing) : ICommand<ApplicationResult<ParkPricing>>;
