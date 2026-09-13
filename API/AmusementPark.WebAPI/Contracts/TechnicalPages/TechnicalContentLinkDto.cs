using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalContentLinkDto
{
    public string Url { get; set; } = string.Empty;

    public List<LocalizedTextDto> Label { get; set; } = new();
}
