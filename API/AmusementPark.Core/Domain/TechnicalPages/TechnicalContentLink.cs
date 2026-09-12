using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.TechnicalPages;

public sealed class TechnicalContentLink
{
    public string Url { get; set; } = string.Empty;

    public List<LocalizedText> Label { get; set; } = new();
}
