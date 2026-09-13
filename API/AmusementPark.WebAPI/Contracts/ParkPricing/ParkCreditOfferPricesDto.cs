using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkPricing;

public sealed class ParkCreditOfferPricesDto
{
    public decimal? OnlinePrice { get; set; }

    public decimal? GatePrice { get; set; }
}
