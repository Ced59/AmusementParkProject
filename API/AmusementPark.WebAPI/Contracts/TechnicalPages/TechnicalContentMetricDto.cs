using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalContentMetricDto
{
    public List<LocalizedTextDto> Label { get; set; } = new();

    public List<LocalizedTextDto> Value { get; set; } = new();

    public List<LocalizedTextDto> HelpText { get; set; } = new();
}
