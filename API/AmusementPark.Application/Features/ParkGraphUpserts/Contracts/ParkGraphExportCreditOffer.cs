using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportCreditOffer
{
    public string? Id { get; init; }

    public string UnitCode { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public List<LocalizedText> Labels { get; init; } = new();

    public ParkGraphExportCreditOfferPrices Prices { get; init; } = new();

    public string? ValidFrom { get; init; }

    public string? ValidTo { get; init; }

    public string? PurchaseUrl { get; init; }

    public List<LocalizedText> Conditions { get; init; } = new();

    public int SortOrder { get; init; }
}
