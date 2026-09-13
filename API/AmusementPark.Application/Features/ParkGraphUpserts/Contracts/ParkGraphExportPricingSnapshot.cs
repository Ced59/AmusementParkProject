using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportPricingSnapshot
{
    public string? Id { get; init; }

    public int Year { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public List<LocalizedText> Notes { get; init; } = new();

    public DateTime? LastVerifiedAtUtc { get; init; }

    public List<ParkGraphExportAdmissionPriceOffer> AdmissionOffers { get; init; } = new();

    public List<ParkGraphExportAnnualPassOffer> AnnualPasses { get; init; } = new();

    public List<ParkGraphExportParkingPriceOffer> ParkingOffers { get; init; } = new();

    public List<ParkGraphExportCreditOffer> CreditOffers { get; init; } = new();
}
