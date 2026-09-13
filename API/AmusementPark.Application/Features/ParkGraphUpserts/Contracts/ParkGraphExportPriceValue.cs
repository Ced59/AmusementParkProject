using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportPriceValue
{
    public ParkPricingMode Mode { get; init; }

    public decimal? Amount { get; init; }

    public decimal? MinimumAmount { get; init; }

    public decimal? MaximumAmount { get; init; }
}
