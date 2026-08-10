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
}

public sealed class ParkGraphExportAdmissionPriceOffer
{
    public string? Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string AudienceCategory { get; init; } = string.Empty;

    public List<LocalizedText> Labels { get; init; } = new();

    public ParkGraphExportPriceValue? OnlinePrice { get; init; }

    public ParkGraphExportPriceValue? GatePrice { get; init; }

    public string? ValidFrom { get; init; }

    public string? ValidTo { get; init; }

    public string? PurchaseUrl { get; init; }

    public List<LocalizedText> Conditions { get; init; } = new();

    public int SortOrder { get; init; }
}

public sealed class ParkGraphExportAnnualPassOffer
{
    public string? Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public List<LocalizedText> Names { get; init; } = new();

    public ParkGraphExportPriceValue? OnlinePrice { get; init; }

    public ParkGraphExportPriceValue? GatePrice { get; init; }

    public string? ValidFrom { get; init; }

    public string? ValidTo { get; init; }

    public string? PurchaseUrl { get; init; }

    public List<LocalizedText> Conditions { get; init; } = new();

    public int SortOrder { get; init; }
}

public sealed class ParkGraphExportParkingPriceOffer
{
    public string? Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public List<LocalizedText> Labels { get; init; } = new();

    public ParkGraphExportPriceValue? OnlinePrice { get; init; }

    public ParkGraphExportPriceValue? GatePrice { get; init; }

    public string? ValidFrom { get; init; }

    public string? ValidTo { get; init; }

    public string? PurchaseUrl { get; init; }

    public List<LocalizedText> Conditions { get; init; } = new();

    public int SortOrder { get; init; }
}

public sealed class ParkGraphExportPriceValue
{
    public ParkPricingMode Mode { get; init; }

    public decimal? Amount { get; init; }

    public decimal? MinimumAmount { get; init; }

    public decimal? MaximumAmount { get; init; }
}
