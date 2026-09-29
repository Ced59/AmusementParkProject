using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.StandaloneAttractions.Commands;

public sealed record UpsertStandaloneAttractionPricingCommand(
    ParkPricingEntity Pricing,
    bool PreserveHistoricalSnapshots = false,
    bool PreserveCreditOffers = false) : ICommand<ApplicationResult<ParkPricingEntity>>;
