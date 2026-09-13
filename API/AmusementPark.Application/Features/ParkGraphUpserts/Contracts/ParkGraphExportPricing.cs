using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportPricing
{
    public string ParkId { get; init; } = string.Empty;

    public string CurrencyCode { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public string? PurchaseUrl { get; init; }

    public List<LocalizedText> Notes { get; init; } = new();

    public DateTime? LastVerifiedAtUtc { get; init; }

    public List<ParkGraphExportAdmissionPriceOffer> AdmissionOffers { get; init; } = new();

    public List<ParkGraphExportAnnualPassOffer> AnnualPasses { get; init; } = new();

    public List<ParkGraphExportParkingPriceOffer> ParkingOffers { get; init; } = new();

    public List<ParkGraphExportCreditOffer> CreditOffers { get; init; } = new();

    public List<ParkGraphExportPricingSnapshot> HistoricalSnapshots { get; init; } = new();
}
