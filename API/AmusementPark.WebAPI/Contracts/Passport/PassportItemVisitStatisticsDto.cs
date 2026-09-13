namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportItemVisitStatisticsDto
{
    public string VisitId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public long RideCount { get; init; }

    public PassportItemRatingCoverageDto RatingCoverage { get; init; } =
        new PassportItemRatingCoverageDto();

    public PassportItemHistoricalRatingsDto? HistoricalRatings { get; init; }
}
