using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Agrégat de lecture pré-calculé pour les moyennes et classements publics.
/// </summary>
public sealed class RatingAggregate : AuditableEntity
{
    public RatingTargetType TargetType { get; set; }

    public string TargetId { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public ParkItemCategory? ParkItemCategory { get; set; }

    public ParkItemType? ParkItemType { get; set; }

    public long RatingCount { get; set; }

    public long? UniqueContributorCount { get; set; }

    public double RatingSum { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public DateTime? LastRatedAtUtc { get; set; }

    public long? MutationVersion { get; set; }

    public long? CalculatedVersion { get; set; }

    public bool? SourceIntegrityIsValid { get; set; }

    public bool? IsCalculationCurrent
    {
        get
        {
            if (!this.MutationVersion.HasValue || !this.CalculatedVersion.HasValue)
            {
                return null;
            }

            return IsCalculationCurrentForVersions(
                this.MutationVersion.Value,
                this.CalculatedVersion.Value);
        }
    }

    public bool? IsIntegrityVerified
    {
        get
        {
            if (this.IsCalculationCurrent == false || this.SourceIntegrityIsValid == false)
            {
                return false;
            }

            if (this.IsCalculationCurrent != true || this.SourceIntegrityIsValid != true)
            {
                return null;
            }

            return true;
        }
    }

    public static bool IsCalculationCurrentForVersions(long mutationVersion, long calculatedVersion)
    {
        return mutationVersion >= 0
            && calculatedVersion == mutationVersion;
    }

    public static bool HasValidSourceProjection(
        long ratingCount,
        long? uniqueContributorCount,
        double ratingSum,
        double averageRating,
        double bayesianScore,
        long sourceRatingObservationCount,
        long sourceUniqueContributorCount,
        double sourceRatingSum)
    {
        if (ratingCount < 0
            || !uniqueContributorCount.HasValue
            || uniqueContributorCount.Value < 0
            || sourceRatingObservationCount < 0
            || sourceUniqueContributorCount < 0
            || !double.IsFinite(ratingSum)
            || !double.IsFinite(averageRating)
            || !double.IsFinite(bayesianScore)
            || !double.IsFinite(sourceRatingSum))
        {
            return false;
        }

        double expectedAverage = RatingScoreCalculator.CalculateAverage(
            sourceRatingSum,
            sourceRatingObservationCount);
        double expectedBayesianScore = RatingScoreCalculator.CalculateBayesianScore(
            sourceRatingSum,
            sourceRatingObservationCount);

        return ratingCount == sourceRatingObservationCount
            && uniqueContributorCount.Value == sourceUniqueContributorCount
            && ratingSum.Equals(sourceRatingSum)
            && averageRating.Equals(expectedAverage)
            && bayesianScore.Equals(expectedBayesianScore);
    }
}
