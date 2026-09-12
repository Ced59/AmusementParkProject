using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.TechnicalPages;

public sealed class TechnicalContentListItem
{
    public List<LocalizedText> Texts { get; set; } = new();
}
