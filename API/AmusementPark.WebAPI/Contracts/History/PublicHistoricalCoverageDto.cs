namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalCoverageDto
{
    public int TotalSubjectCount { get; set; }

    public int ReliablePeriodSubjectCount { get; set; }

    public int PartialPeriodSubjectCount { get; set; }

    public int UndatedSubjectCount { get; set; }

    public PublicHistoricalFieldCoverageDto Name { get; set; } = new PublicHistoricalFieldCoverageDto();

    public PublicHistoricalFieldCoverageDto Zone { get; set; } = new PublicHistoricalFieldCoverageDto();

    public DateTime? LastReviewedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;
}
