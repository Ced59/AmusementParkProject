using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkPricing;

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
