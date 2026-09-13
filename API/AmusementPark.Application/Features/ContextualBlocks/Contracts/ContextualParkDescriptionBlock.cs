using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualParkDescriptionBlock
{
    public string ParkId { get; init; } = string.Empty;

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();
}
