namespace AmusementPark.WebAPI.Contracts.Passport;

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

    public long RideCount { get; init; }

    public long VisitCount { get; init; }

    public PassportItemRatingCoverageDto RatingCoverage { get; init; } =
        new PassportItemRatingCoverageDto();

    public PassportItemExperienceDto? FirstExperience { get; init; }

    public PassportItemExperienceDto? LastExperience { get; init; }

    public PassportItemHistoricalRatingsDto? HistoricalRatings { get; init; }

    public double? CurrentGlobalRating { get; init; }

    public double? CurrentGlobalMinusHistoricalAverage { get; init; }
}
