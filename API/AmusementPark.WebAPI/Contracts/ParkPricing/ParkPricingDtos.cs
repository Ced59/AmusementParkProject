using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkPricing;

public sealed class ParkPricingDto
{
    public string ParkId { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? PurchaseUrl { get; set; }

    public string? Notes { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime? CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public IReadOnlyCollection<ParkAdmissionPriceOfferDto> AdmissionOffers { get; set; } = Array.Empty<ParkAdmissionPriceOfferDto>();

    public IReadOnlyCollection<ParkAnnualPassOfferDto> AnnualPasses { get; set; } = Array.Empty<ParkAnnualPassOfferDto>();

    public IReadOnlyCollection<ParkParkingPriceOfferDto> ParkingOffers { get; set; } = Array.Empty<ParkParkingPriceOfferDto>();
}

public sealed class ParkAdmissionPriceOfferDto
{
    public string? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string AudienceCategory { get; set; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextDto> Labels { get; set; } = Array.Empty<LocalizedTextDto>();

    public ParkPriceValueDto? OnlinePrice { get; set; }

    public ParkPriceValueDto? GatePrice { get; set; }

    public string? ValidFrom { get; set; }

    public string? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Conditions { get; set; } = Array.Empty<LocalizedTextDto>();

    public int SortOrder { get; set; }
}

public sealed class ParkAnnualPassOfferDto
{
    public string? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextDto> Names { get; set; } = Array.Empty<LocalizedTextDto>();

    public ParkPriceValueDto? OnlinePrice { get; set; }

    public ParkPriceValueDto? GatePrice { get; set; }

    public string? ValidFrom { get; set; }

    public string? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Conditions { get; set; } = Array.Empty<LocalizedTextDto>();

    public int SortOrder { get; set; }
}

public sealed class ParkParkingPriceOfferDto
{
    public string? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextDto> Labels { get; set; } = Array.Empty<LocalizedTextDto>();

    public ParkPriceValueDto? OnlinePrice { get; set; }

    public ParkPriceValueDto? GatePrice { get; set; }

    public string? ValidFrom { get; set; }

    public string? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Conditions { get; set; } = Array.Empty<LocalizedTextDto>();

    public int SortOrder { get; set; }
}

public sealed class ParkPriceValueDto
{
    public string Mode { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    public decimal? MinimumAmount { get; set; }

    public decimal? MaximumAmount { get; set; }
}
