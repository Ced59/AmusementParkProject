namespace AmusementPark.WebAPI.Contracts.History;

public sealed class SubmitHistoricalExistenceReportRequestDto
{
    public string ClaimedName { get; init; } = string.Empty;
    public string? SourceUrl { get; init; }
    public string? SourceReference { get; init; }
    public string? Details { get; init; }
}
