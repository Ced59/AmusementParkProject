using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkPricing.Commands;

public sealed record UpsertParkPricingCommand(
    ParkPricingEntity Pricing,
    bool PreserveHistoricalSnapshots = false) : ICommand<ApplicationResult<ParkPricingEntity>>;
