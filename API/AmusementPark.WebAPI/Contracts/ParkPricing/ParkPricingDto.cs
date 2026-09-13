using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkPricing;

public sealed class ParkPricingDto
{
    public string ParkId { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? PurchaseUrl { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Notes { get; set; } = Array.Empty<LocalizedTextDto>();

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime? CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public IReadOnlyCollection<ParkAdmissionPriceOfferDto> AdmissionOffers { get; set; } = Array.Empty<ParkAdmissionPriceOfferDto>();

    public IReadOnlyCollection<ParkAnnualPassOfferDto> AnnualPasses { get; set; } = Array.Empty<ParkAnnualPassOfferDto>();

    public IReadOnlyCollection<ParkParkingPriceOfferDto> ParkingOffers { get; set; } = Array.Empty<ParkParkingPriceOfferDto>();

    public IReadOnlyCollection<ParkCreditOfferDto>? CreditOffers { get; set; }

    public IReadOnlyCollection<ParkPricingSnapshotDto>? HistoricalSnapshots { get; set; }
}
