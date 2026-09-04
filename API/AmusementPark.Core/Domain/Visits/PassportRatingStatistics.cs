using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportRatingStatistics(
    long RatingCount,
    long HalfStepSum,
    double Average,
    double Median,
    double Minimum,
    double Maximum,
    double PopulationStandardDeviation);

/// <summary>
/// Calcule une distribution exacte à partir de notes représentées en demi-points.
/// Les valeurs constituent la population privée complète du périmètre demandé.
/// </summary>
public static class PassportRatingStatisticsCalculator
{
    public static PassportRatingStatistics? Calculate(
        IReadOnlyCollection<RatingValue> ratings)
    {
        ArgumentNullException.ThrowIfNull(ratings);
        if (ratings.Count == 0)
        {
            return null;
        }

        byte[] halfSteps = ratings
            .Select(static rating => rating.HalfSteps)
            .OrderBy(static value => value)
            .ToArray();
        long halfStepSum = halfSteps.Sum(static value => (long)value);
        double averageHalfSteps = halfStepSum / (double)halfSteps.Length;
        double squaredDeviationSum = halfSteps.Sum(value =>
        {
            double deviation = value - averageHalfSteps;
            return deviation * deviation;
        });
        int middleIndex = halfSteps.Length / 2;
        double median = halfSteps.Length % 2 == 0
            ? (halfSteps[middleIndex - 1] + halfSteps[middleIndex]) / 4d
            : halfSteps[middleIndex] / 2d;

        return new PassportRatingStatistics(
            halfSteps.LongLength,
            halfStepSum,
            averageHalfSteps / 2d,
            median,
            halfSteps[0] / 2d,
            halfSteps[^1] / 2d,
            Math.Sqrt(squaredDeviationSum / halfSteps.Length) / 2d);
    }
}
