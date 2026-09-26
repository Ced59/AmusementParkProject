namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalFieldCoverageDto
{
    public int DocumentedSubjectCount { get; set; }

    public int ApplicableSubjectCount { get; set; }

    public decimal Percentage { get; set; }

    public bool IsComplete { get; set; }
}
