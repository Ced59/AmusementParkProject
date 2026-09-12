using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

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
