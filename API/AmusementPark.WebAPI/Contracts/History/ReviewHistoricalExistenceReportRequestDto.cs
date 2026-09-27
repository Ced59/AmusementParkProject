namespace AmusementPark.WebAPI.Contracts.History;

public sealed class ReviewHistoricalExistenceReportRequestDto
{
    public string Decision { get; init; } = string.Empty;
    public string? DecisionNote { get; init; }
    public long ExpectedRevision { get; init; }
}
