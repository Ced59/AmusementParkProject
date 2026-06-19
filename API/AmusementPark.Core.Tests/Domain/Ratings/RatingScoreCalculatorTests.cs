using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RatingScoreCalculatorTests
{
    [Fact]
    public void CalculateAverage_WhenNoRating_ShouldReturnZero()
    {
        double average = RatingScoreCalculator.CalculateAverage(0d, 0);

        Assert.Equal(0d, average);
    }

    [Fact]
    public void CalculateAverage_WhenRatingsExist_ShouldReturnRawAverage()
    {
        double average = RatingScoreCalculator.CalculateAverage(18d, 4);

        Assert.Equal(4.5d, average);
    }

    [Fact]
    public void CalculateBayesianScore_WhenNoRating_ShouldReturnPriorMean()
    {
        double score = RatingScoreCalculator.CalculateBayesianScore(0d, 0);

        Assert.Equal(RatingScoreCalculator.PriorMean, score);
    }

    [Fact]
    public void CalculateBayesianScore_WhenRatingsExist_ShouldSmoothTowardPriorMean()
    {
        double score = RatingScoreCalculator.CalculateBayesianScore(10d, 2);

        Assert.Equal(3.75d, score);
    }

    [Fact]
    public void CalculateCompositeParkScore_WhenParkAndItemsExist_ShouldFavorDirectParkRating()
    {
        double score = RatingScoreCalculator.CalculateCompositeParkScore(4.5d, 3.5d);

        Assert.Equal(4.2d, score, 6);
    }

    [Theory]
    [InlineData(0.5d)]
    [InlineData(1d)]
    [InlineData(2.5d)]
    [InlineData(5d)]
    public void IsValidUserRating_WhenValueUsesHalfStepAndAllowedRange_ShouldReturnTrue(double value)
    {
        bool isValid = RatingScoreCalculator.IsValidUserRating(value);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.25d)]
    [InlineData(4.25d)]
    [InlineData(5.5d)]
    public void IsValidUserRating_WhenValueIsOutsideRules_ShouldReturnFalse(double value)
    {
        bool isValid = RatingScoreCalculator.IsValidUserRating(value);

        Assert.False(isValid);
    }
}
