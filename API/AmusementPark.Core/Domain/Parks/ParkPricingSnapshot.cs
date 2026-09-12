using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkPricingSnapshot
{
    public string? Id { get; set; }

    public int Year { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public List<LocalizedText> Notes { get; set; } = new();

    public DateTime? LastVerifiedAtUtc { get; set; }

    public List<ParkAdmissionPriceOffer> AdmissionOffers { get; set; } = new();

    public List<ParkAnnualPassOffer> AnnualPasses { get; set; } = new();

    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();

    public List<ParkCreditOffer> CreditOffers { get; set; } = new();

    public bool HasPricedOffers()
    {
        return this.AdmissionOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)
            || this.AnnualPasses.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)
            || this.ParkingOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)
            || this.CreditOffers.Any(static offer => offer.Prices.OnlinePrice.HasValue || offer.Prices.GatePrice.HasValue);
    }
}
