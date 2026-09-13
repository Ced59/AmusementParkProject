using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkPricing;

public sealed class ParkPricingSnapshotDto
{
    public string? Id { get; set; }

    public int Year { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Notes { get; set; } = Array.Empty<LocalizedTextDto>();

    public DateTime? LastVerifiedAtUtc { get; set; }

    public IReadOnlyCollection<ParkAdmissionPriceOfferDto> AdmissionOffers { get; set; } = Array.Empty<ParkAdmissionPriceOfferDto>();

    public IReadOnlyCollection<ParkAnnualPassOfferDto> AnnualPasses { get; set; } = Array.Empty<ParkAnnualPassOfferDto>();

    public IReadOnlyCollection<ParkParkingPriceOfferDto> ParkingOffers { get; set; } = Array.Empty<ParkParkingPriceOfferDto>();

    public IReadOnlyCollection<ParkCreditOfferDto> CreditOffers { get; set; } = Array.Empty<ParkCreditOfferDto>();
}
