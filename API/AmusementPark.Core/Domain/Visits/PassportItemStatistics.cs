using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Observation active d'un tour terminé, suffisante pour les statistiques privées d'un élément.
/// </summary>
public sealed record PassportItemRideObservation
{
    public PassportItemRideObservation(
        string visitId,
        VisitDate visitDate,
        RatingValue? assessment)
    {
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        this.Assessment = assessment;
    }

    public string VisitId { get; }

    public VisitDate VisitDate { get; }

    public RatingValue? Assessment { get; }
}

public sealed record PassportItemExperience(
    string VisitId,
    VisitDate VisitDate);

public sealed record PassportItemRatingStatistics(
    long RatingCount,
    long HalfStepSum,
    double Average,
    double Median,
    double Minimum,
    double Maximum,
    double PopulationStandardDeviation);

public sealed record PassportItemStatistics(
    long RideCount,
    long VisitCount,
    long RatedRideCount,
    double RatingCoverageRate,
    PassportItemExperience? FirstExperience,
    PassportItemExperience? LastExperience,
    PassportItemRatingStatistics? Ratings,
    RatingValue? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage);

/// <summary>
/// Calcule les statistiques d'un élément à partir des observations actives.
/// Les observations représentent toute la population privée connue : la dispersion est donc
/// l'écart-type de population. Les dates partielles restent des <see cref="VisitDate"/>.
/// </summary>
public static class PassportItemStatisticsCalculator
{
    public static PassportItemStatistics Calculate(
        IReadOnlyCollection<PassportItemRideObservation> observations,
        RatingValue? currentGlobalRating)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count == 0)
        {
            return new PassportItemStatistics(
                0,
                0,
                0,
                0d,
                null,
                null,
                null,
                currentGlobalRating,
                null);
        }

        PassportItemRideObservation[] orderedObservations = observations
            .OrderBy(static observation => observation.VisitDate.ChronologicalOrderValue)
            .ThenBy(static observation => observation.VisitId, StringComparer.Ordinal)
            .ToArray();
        byte[] ratingHalfSteps = observations
            .Where(static observation => observation.Assessment.HasValue)
            .Select(static observation => observation.Assessment!.Value.HalfSteps)
            .OrderBy(static halfSteps => halfSteps)
            .ToArray();
        long rideCount = observations.Count;
        long ratedRideCount = ratingHalfSteps.LongLength;
        PassportItemRatingStatistics? ratings = CalculateRatings(ratingHalfSteps);

        return new PassportItemStatistics(
            rideCount,
            observations.Select(static observation => observation.VisitId)
                .Distinct(StringComparer.Ordinal)
                .LongCount(),
            ratedRideCount,
            ratedRideCount / (double)rideCount,
            ToExperience(orderedObservations[0]),
            ToExperience(orderedObservations[^1]),
            ratings,
            currentGlobalRating,
            currentGlobalRating.HasValue && ratings is not null
                ? currentGlobalRating.Value.DoubleValue - ratings.Average
                : null);
    }

    private static PassportItemExperience ToExperience(
        PassportItemRideObservation observation)
    {
        return new PassportItemExperience(observation.VisitId, observation.VisitDate);
    }

    private static PassportItemRatingStatistics? CalculateRatings(byte[] ratingHalfSteps)
    {
        if (ratingHalfSteps.Length == 0)
        {
            return null;
        }

        long halfStepSum = ratingHalfSteps.Sum(static value => (long)value);
        double averageHalfSteps = halfStepSum / (double)ratingHalfSteps.Length;
        double squaredDeviationSum = ratingHalfSteps.Sum(value =>
        {
            double deviation = value - averageHalfSteps;
            return deviation * deviation;
        });
        int middleIndex = ratingHalfSteps.Length / 2;
        double median = ratingHalfSteps.Length % 2 == 0
            ? (ratingHalfSteps[middleIndex - 1] + ratingHalfSteps[middleIndex]) / 4d
            : ratingHalfSteps[middleIndex] / 2d;

        return new PassportItemRatingStatistics(
            ratingHalfSteps.LongLength,
            halfStepSum,
            averageHalfSteps / 2d,
            median,
            ratingHalfSteps[0] / 2d,
            ratingHalfSteps[^1] / 2d,
            Math.Sqrt(squaredDeviationSum / ratingHalfSteps.Length) / 2d);
    }
}
