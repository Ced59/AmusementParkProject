namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Règle métier de calcul des moyennes et du score fiable de classement.
/// </summary>
public static class RatingScoreCalculator
{
    public const double PriorMean = 3.5d;
    public const int PriorWeight = 10;

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

    public static bool IsValidUserRating(double value)
    {
        if (value < 0.5d || value > 5d)
        {
            return false;
        }

        double doubledValue = value * 2d;
        return Math.Abs(doubledValue - Math.Round(doubledValue, MidpointRounding.AwayFromZero)) < 0.000001d;
    }
}
