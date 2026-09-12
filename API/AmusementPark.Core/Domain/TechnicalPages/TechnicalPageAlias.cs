using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.TechnicalPages;

public sealed class TechnicalPageAlias
{
    public string CategoryKey { get; set; } = string.Empty;

    public List<LocalizedText> Labels { get; set; } = new();
}
