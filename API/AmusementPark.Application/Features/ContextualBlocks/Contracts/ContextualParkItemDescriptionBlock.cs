using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualParkItemDescriptionBlock
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkItemId { get; init; } = string.Empty;

    public string? ZoneId { get; init; }

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();
}
