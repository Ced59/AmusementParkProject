using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalContentTableDto
{
    public List<TechnicalContentTableCellDto> Headers { get; set; } = new();

    public List<TechnicalContentTableRowDto> Rows { get; set; } = new();
}
