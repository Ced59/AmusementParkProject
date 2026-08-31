using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RatingAggregateTests
{
    [Fact]
    public void HasValidSourceProjection_WhenEveryFactMatches_ShouldReturnTrue()
    {
        bool result = RatingAggregate.HasValidSourceProjection(
            ratingCount: 10,
            uniqueContributorCount: 10,
            ratingSum: 45d,
            averageRating: 4.5d,
            bayesianScore: 4d,
            sourceRatingObservationCount: 10,
            sourceUniqueContributorCount: 10,
            sourceRatingSum: 45d);

        Assert.True(result);
    }

    [Theory]
    [InlineData(44d, 4.5d, 4d)]
    [InlineData(45d, 4.4d, 4d)]
    [InlineData(45d, 4.5d, 4.1d)]
    public void HasValidSourceProjection_WhenAValueDiverges_ShouldReturnFalse(
        double ratingSum,
        double averageRating,
        double bayesianScore)
    {
        bool result = RatingAggregate.HasValidSourceProjection(
            ratingCount: 10,
            uniqueContributorCount: 10,
            ratingSum,
            averageRating,
            bayesianScore,
            sourceRatingObservationCount: 10,
            sourceUniqueContributorCount: 10,
            sourceRatingSum: 45d);

        Assert.False(result);
    }

    [Fact]
    public void TryResolveVerifiedSourceProjection_WhenLegacyContributorCountIsMissing_ShouldUseSourceCount()
    {
        bool result = RatingAggregate.TryResolveVerifiedSourceProjection(
            ratingCount: 10,
            uniqueContributorCount: null,
            ratingSum: 45d,
            averageRating: 4.5d,
            bayesianScore: 4d,
            sourceRatingObservationCount: 10,
            sourceUniqueContributorCount: 8,
            sourceRatingSum: 45d,
            out long verifiedUniqueContributorCount);

        Assert.True(result);
        Assert.Equal(8, verifiedUniqueContributorCount);
    }

    [Fact]
    public void TryResolveVerifiedSourceProjection_WhenPersistedContributorCountDiffers_ShouldRejectProjection()
    {
        bool result = RatingAggregate.TryResolveVerifiedSourceProjection(
            ratingCount: 10,
            uniqueContributorCount: 9,
            ratingSum: 45d,
            averageRating: 4.5d,
            bayesianScore: 4d,
            sourceRatingObservationCount: 10,
            sourceUniqueContributorCount: 8,
            sourceRatingSum: 45d,
            out long verifiedUniqueContributorCount);

        Assert.False(result);
        Assert.Equal(0, verifiedUniqueContributorCount);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, null, null)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void IsIntegrityVerified_ShouldRequireCurrentCalculationAndVerifiedSource(
        bool calculationIsCurrent,
        bool? sourceIntegrityIsValid,
        bool? expected)
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            MutationVersion = 2,
            CalculatedVersion = calculationIsCurrent ? 2 : 1,
            SourceIntegrityIsValid = sourceIntegrityIsValid,
        };

        Assert.Equal(expected, aggregate.IsIntegrityVerified);
    }
}
