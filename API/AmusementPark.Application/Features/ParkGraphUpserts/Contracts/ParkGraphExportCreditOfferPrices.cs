using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportCreditOfferPrices
{
    public decimal? OnlinePrice { get; init; }

    public decimal? GatePrice { get; init; }
}
