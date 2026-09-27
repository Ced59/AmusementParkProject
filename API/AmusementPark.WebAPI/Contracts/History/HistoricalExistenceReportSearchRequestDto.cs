namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoricalExistenceReportSearchRequestDto
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public string? Status { get; init; }
    public string? ParkId { get; init; }
}
