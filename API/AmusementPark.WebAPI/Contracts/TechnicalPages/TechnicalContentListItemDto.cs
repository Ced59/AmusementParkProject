using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalContentListItemDto
{
    public List<LocalizedTextDto> Texts { get; set; } = new();
}
