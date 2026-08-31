namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Business rule for rating averages and reliable ranking scores.
/// </summary>
public static class RatingScoreCalculator
{
    public const double PriorMean = 3.5d;
    public const int PriorWeight = 10;
    public const double ParkDirectScoreWeight = 0.7d;
    public const double ParkItemsScoreWeight = 0.3d;

    public static double CalculateAverage(double ratingSum, long ratingCount)
    {
        if (ratingCount <= 0)
        {
            return 0d;
        }

        return ratingSum / ratingCount;
    }

    public static double CalculateBayesianScore(double ratingSum, long ratingCount)
    {
        if (ratingCount <= 0)
        {
            return PriorMean;
        }

        return (ratingSum + (PriorMean * PriorWeight)) / (ratingCount + PriorWeight);
    }

    public static long SumHalfSteps(IEnumerable<RatingValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        long sumHalfSteps = 0;
        foreach (RatingValue value in values)
        {
            sumHalfSteps = checked(sumHalfSteps + value.HalfSteps);
        }

        return sumHalfSteps;
    }

    public static double CalculateAverageFromHalfSteps(long sumHalfSteps, long ratingCount)
    {
        if (sumHalfSteps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sumHalfSteps));
        }

        if (ratingCount <= 0)
        {
            return 0d;
        }

        return (sumHalfSteps / 2d) / ratingCount;
    }

    public static double CalculateBayesianScoreFromHalfSteps(long sumHalfSteps, long ratingCount)
    {
        if (sumHalfSteps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sumHalfSteps));
        }

        if (ratingCount <= 0)
        {
            return PriorMean;
        }

        return ((sumHalfSteps / 2d) + (PriorMean * PriorWeight)) / (ratingCount + PriorWeight);
    }

    public static double CalculateCompositeParkScore(double? directParkScore, double? parkItemsScore)
    {
        if (directParkScore.HasValue && parkItemsScore.HasValue)
        {
            return (directParkScore.Value * ParkDirectScoreWeight) + (parkItemsScore.Value * ParkItemsScoreWeight);
        }

        if (directParkScore.HasValue)
        {
            return directParkScore.Value;
        }

        if (parkItemsScore.HasValue)
        {
            return parkItemsScore.Value;
        }

        return PriorMean;
    }

    public static double CalculateCategoryBalancedItemsScore(IReadOnlyCollection<double> categoryScores)
    {
        if (categoryScores.Count == 0)
        {
            return PriorMean;
        }

        return categoryScores.Average();
    }

    public static bool IsValidUserRating(double value)
    {
        return RatingValue.TryFromDouble(value, out RatingValue _, out string? _);
    }
}
