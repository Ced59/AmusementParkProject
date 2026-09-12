using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

public enum ParkPricingMode
{
    Fixed = 0,
    Range = 1,
    Dynamic = 2,
}
public sealed class ParkPricing
{
    public string? Id { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? PurchaseUrl { get; set; }

    public List<LocalizedText> Notes { get; set; } = new();

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<ParkAdmissionPriceOffer> AdmissionOffers { get; set; } = new();

    public List<ParkAnnualPassOffer> AnnualPasses { get; set; } = new();

    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();

    public List<ParkCreditOffer> CreditOffers { get; set; } = new();

    public List<ParkPricingSnapshot> HistoricalSnapshots { get; set; } = new();

    public ParkPricing FilterOffersValidOn(DateOnly date)
    {
        return new ParkPricing
        {
            Id = this.Id,
            ParkId = this.ParkId,
            CurrencyCode = this.CurrencyCode,
            SourceUrl = this.SourceUrl,
            PurchaseUrl = this.PurchaseUrl,
            Notes = this.Notes.ToList(),
            LastVerifiedAtUtc = this.LastVerifiedAtUtc,
            CreatedAtUtc = this.CreatedAtUtc,
            UpdatedAtUtc = this.UpdatedAtUtc,
            AdmissionOffers = this.AdmissionOffers
                .Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date))
                .ToList(),
            AnnualPasses = this.AnnualPasses
                .Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date))
                .ToList(),
            ParkingOffers = this.ParkingOffers
                .Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date))
                .ToList(),
            CreditOffers = this.CreditOffers
                .Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date))
                .ToList(),
            HistoricalSnapshots = this.HistoricalSnapshots
                .OrderByDescending(static snapshot => snapshot.Year)
                .ToList(),
        };
    }

    public bool HasPricedOffersValidOn(DateOnly date)
    {
        return this.AdmissionOffers.Any(offer => HasPrice(offer.OnlinePrice, offer.GatePrice)
                && IsValidOn(offer.ValidFrom, offer.ValidTo, date))
            || this.AnnualPasses.Any(offer => HasPrice(offer.OnlinePrice, offer.GatePrice)
                && IsValidOn(offer.ValidFrom, offer.ValidTo, date))
            || this.ParkingOffers.Any(offer => HasPrice(offer.OnlinePrice, offer.GatePrice)
                && IsValidOn(offer.ValidFrom, offer.ValidTo, date))
            || this.CreditOffers.Any(offer => (offer.Prices.OnlinePrice.HasValue || offer.Prices.GatePrice.HasValue)
                && IsValidOn(offer.ValidFrom, offer.ValidTo, date));
    }

    private static bool HasPrice(ParkPriceValue? onlinePrice, ParkPriceValue? gatePrice)
    {
        return onlinePrice is not null || gatePrice is not null;
    }

    private static bool IsValidOn(DateOnly? validFrom, DateOnly? validTo, DateOnly date)
    {
        return (!validFrom.HasValue || validFrom.Value <= date)
            && (!validTo.HasValue || validTo.Value >= date);
    }
}
