namespace AmusementPark.Core.Domain.TechnicalPages;

public sealed class TechnicalContentTable
{
    public List<TechnicalContentTableCell> Headers { get; set; } = new();

    public List<TechnicalContentTableRow> Rows { get; set; } = new();
}
