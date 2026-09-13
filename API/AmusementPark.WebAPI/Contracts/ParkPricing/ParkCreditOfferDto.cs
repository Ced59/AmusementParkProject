using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkPricing;

public sealed class ParkCreditOfferDto
{
    public string? Id { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Labels { get; set; } = Array.Empty<LocalizedTextDto>();

    public ParkCreditOfferPricesDto? Prices { get; set; }

    public string? ValidFrom { get; set; }

    public string? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Conditions { get; set; } = Array.Empty<LocalizedTextDto>();

    public int SortOrder { get; set; }
}
