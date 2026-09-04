using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportStatisticsHttpMappers
{
    public static PassportItemStatisticsDto ToHttp(
        this PassportItemStatisticsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PassportItemStatisticsDto
        {
            ParkItemId = result.ParkItemId,
            RideCount = result.RideCount,
            VisitCount = result.VisitCount,
            RatingCoverage = new PassportItemRatingCoverageDto
            {
                RatedRideCount = result.RatingCoverage.RatedRideCount,
                TotalRideCount = result.RatingCoverage.TotalRideCount,
                Rate = result.RatingCoverage.Rate,
            },
            FirstExperience = ToHttp(result.FirstExperience),
            LastExperience = ToHttp(result.LastExperience),
            HistoricalRatings = result.HistoricalRatings is null
                ? null
                : new PassportItemHistoricalRatingsDto
                {
                    RatingCount = result.HistoricalRatings.RatingCount,
                    Average = result.HistoricalRatings.Average,
                    Median = result.HistoricalRatings.Median,
                    Minimum = result.HistoricalRatings.Minimum,
                    Maximum = result.HistoricalRatings.Maximum,
                    PopulationStandardDeviation =
                        result.HistoricalRatings.PopulationStandardDeviation,
                },
            CurrentGlobalRating = result.CurrentGlobalRating,
            CurrentGlobalMinusHistoricalAverage =
                result.CurrentGlobalMinusHistoricalAverage,
        };
    }

    private static PassportItemExperienceDto? ToHttp(
        PassportItemExperienceResult? result)
    {
        return result is null
            ? null
            : new PassportItemExperienceDto
            {
                VisitId = result.VisitId,
                Date = new PassportVisitDateDto
                {
                    Year = result.Date.Year,
                    Month = result.Date.Month,
                    Day = result.Date.Day,
                    Precision = (PassportVisitDatePrecisionDto)result.Date.Precision,
                    IsApproximate = result.Date.IsApproximate,
                },
            };
    }
}
