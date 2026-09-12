using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkCreditOffer
{
    public string? Id { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public List<LocalizedText> Labels { get; set; } = new();

    public ParkCreditOfferPrices Prices { get; set; } = new();

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public string? PurchaseUrl { get; set; }

    public List<LocalizedText> Conditions { get; set; } = new();

    public int SortOrder { get; set; }
}
