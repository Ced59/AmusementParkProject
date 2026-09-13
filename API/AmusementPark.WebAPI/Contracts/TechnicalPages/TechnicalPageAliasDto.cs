using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalPageAliasDto
{
    public string CategoryKey { get; set; } = string.Empty;

    public List<LocalizedTextDto> Labels { get; set; } = new();
}
