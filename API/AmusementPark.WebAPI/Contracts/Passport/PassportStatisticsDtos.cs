namespace AmusementPark.WebAPI.Contracts.Passport;

public enum PassportRatingTrendKindDto
{
    Stable = 0,
    Rising = 1,
    Falling = 2,
}

public sealed class PassportItemExperienceDto
{
    public string VisitId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();
}

public sealed class PassportItemRatingCoverageDto
{
    public long RatedRideCount { get; init; }

    public long TotalRideCount { get; init; }

    public double Rate { get; init; }
}

public sealed class PassportItemHistoricalRatingsDto
{
    public long RatingCount { get; init; }

    public double Average { get; init; }

    public double Median { get; init; }

    public double Minimum { get; init; }

    public double Maximum { get; init; }

    public double PopulationStandardDeviation { get; init; }
}

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

public sealed class PassportItemVisitStatisticsDto
{
    public string VisitId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public long RideCount { get; init; }

    public PassportItemRatingCoverageDto RatingCoverage { get; init; } =
        new PassportItemRatingCoverageDto();

    public PassportItemHistoricalRatingsDto? HistoricalRatings { get; init; }
}

public sealed class PassportItemYearStatisticsDto
{
    public int Year { get; init; }

    public long RideCount { get; init; }

    public long VisitCount { get; init; }

    public PassportItemRatingCoverageDto RatingCoverage { get; init; } =
        new PassportItemRatingCoverageDto();

    public PassportItemHistoricalRatingsDto? HistoricalRatings { get; init; }
}

public sealed class PassportItemRatingPointDto
{
    public string RideOccurrenceId { get; init; } = string.Empty;

    public string VisitId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public long SortPosition { get; init; }

    public double Rating { get; init; }
}

public sealed class PassportRatingTrendDto
{
    public PassportRatingTrendKindDto Kind { get; init; }

    public long FirstWindowRatingCount { get; init; }

    public long LastWindowRatingCount { get; init; }

    public double FirstWindowAverage { get; init; }

    public double LastWindowAverage { get; init; }

    public double Delta { get; init; }
}
