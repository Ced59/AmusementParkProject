using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.TechnicalPages;

public sealed class TechnicalContentMetric
{
    public List<LocalizedText> Label { get; set; } = new();

    public List<LocalizedText> Value { get; set; } = new();

    public List<LocalizedText> HelpText { get; set; } = new();
}
