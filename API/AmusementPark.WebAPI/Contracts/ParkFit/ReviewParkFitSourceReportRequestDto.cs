namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ReviewParkFitSourceReportRequestDto
{
    public string Decision { get; init; } = string.Empty;

    public string? DecisionNote { get; init; }

    public long ExpectedRevision { get; init; }
}
