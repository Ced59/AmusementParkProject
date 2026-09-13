namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportItemYearStatisticsDto
{
    public int Year { get; init; }

    public long RideCount { get; init; }

    public long VisitCount { get; init; }

    public PassportItemRatingCoverageDto RatingCoverage { get; init; } =
        new PassportItemRatingCoverageDto();

    public PassportItemHistoricalRatingsDto? HistoricalRatings { get; init; }
}
