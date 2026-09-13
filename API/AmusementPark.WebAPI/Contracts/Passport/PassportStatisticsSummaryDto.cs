namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportStatisticsSummaryDto
{
    public long VisitCount { get; init; }
    public long ApproximateVisitCount { get; init; }
    public PassportRatingCoverageDto ParkRatingCoverage { get; init; } =
        new PassportRatingCoverageDto();
    public PassportRatingDistributionDto? HistoricalParkRatings { get; init; }
    public PassportVisitExperienceDto? FirstVisit { get; init; }
    public PassportVisitExperienceDto? LastVisit { get; init; }
    public PassportRideOutcomeStatisticsDto RideOutcomes { get; init; } =
        new PassportRideOutcomeStatisticsDto();
    public PassportRatingCoverageDto RideRatingCoverage { get; init; } =
        new PassportRatingCoverageDto();
    public PassportRatingDistributionDto? HistoricalRideRatings { get; init; }
    public long DistinctCompletedItemCount { get; init; }
    public long RepeatedCompletedItemCount { get; init; }
    public IReadOnlyCollection<PassportCategoryCoverageDto> CategoryCoverage { get; init; } =
        Array.Empty<PassportCategoryCoverageDto>();
}
