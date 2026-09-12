using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.TechnicalPages;

public sealed class TechnicalContentTableCell
{
    public List<LocalizedText> Texts { get; set; } = new();
}
