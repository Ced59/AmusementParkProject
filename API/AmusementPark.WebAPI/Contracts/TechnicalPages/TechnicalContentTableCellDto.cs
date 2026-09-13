using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalContentTableCellDto
{
    public List<LocalizedTextDto> Texts { get; set; } = new();
}
