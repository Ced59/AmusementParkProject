using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

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
