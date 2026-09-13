namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportItemStatisticsDto
{
    public string ParkItemId { get; init; } = string.Empty;

    public string? ParkItemName { get; init; }

    public long RideCount { get; init; }

    public long VisitCount { get; init; }

    public PassportItemRatingCoverageDto RatingCoverage { get; init; } =
        new PassportItemRatingCoverageDto();

    public PassportItemExperienceDto? FirstExperience { get; init; }

    public PassportItemExperienceDto? LastExperience { get; init; }

    public PassportItemHistoricalRatingsDto? HistoricalRatings { get; init; }

    public double? CurrentGlobalRating { get; init; }

    public double? CurrentGlobalMinusHistoricalAverage { get; init; }

    public IReadOnlyCollection<PassportItemVisitStatisticsDto> ByVisit { get; init; } =
        Array.Empty<PassportItemVisitStatisticsDto>();

    public IReadOnlyCollection<PassportItemYearStatisticsDto> ByYear { get; init; } =
        Array.Empty<PassportItemYearStatisticsDto>();

    public IReadOnlyCollection<PassportItemRatingPointDto> RatingTimeline { get; init; } =
        Array.Empty<PassportItemRatingPointDto>();

    public PassportRatingTrendDto? Trend { get; init; }
}
