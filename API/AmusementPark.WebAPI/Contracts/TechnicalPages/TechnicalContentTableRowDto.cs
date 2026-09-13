using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalContentTableRowDto
{
    public List<TechnicalContentTableCellDto> Cells { get; set; } = new();
}
