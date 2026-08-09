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

    public string? Notes { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<ParkAdmissionPriceOffer> AdmissionOffers { get; set; } = new();

    public List<ParkAnnualPassOffer> AnnualPasses { get; set; } = new();

    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();
}

public sealed class ParkAdmissionPriceOffer
{
    public string? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string AudienceCategory { get; set; } = string.Empty;

    public List<LocalizedText> Labels { get; set; } = new();

    public ParkPriceValue? OnlinePrice { get; set; }

    public ParkPriceValue? GatePrice { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public List<LocalizedText> Conditions { get; set; } = new();

    public int SortOrder { get; set; }
}

public sealed class ParkAnnualPassOffer
{
    public string? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public List<LocalizedText> Names { get; set; } = new();

    public ParkPriceValue? OnlinePrice { get; set; }

    public ParkPriceValue? GatePrice { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public List<LocalizedText> Conditions { get; set; } = new();

    public int SortOrder { get; set; }
}

public sealed class ParkParkingPriceOffer
{
    public string? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public List<LocalizedText> Labels { get; set; } = new();

    public ParkPriceValue? OnlinePrice { get; set; }

    public ParkPriceValue? GatePrice { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public List<LocalizedText> Conditions { get; set; } = new();

    public int SortOrder { get; set; }
}

public sealed class ParkPriceValue
{
    public ParkPricingMode Mode { get; set; }

    public decimal? Amount { get; set; }

    public decimal? MinimumAmount { get; set; }

    public decimal? MaximumAmount { get; set; }
}
