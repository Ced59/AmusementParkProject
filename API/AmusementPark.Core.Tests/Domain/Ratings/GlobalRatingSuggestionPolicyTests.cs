using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class GlobalRatingSuggestionPolicyTests
{
    private static readonly DateTime RatingUpdatedAtUtc =
        new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly GlobalRatingSuggestionPolicy policy = new GlobalRatingSuggestionPolicy();

    [Fact]
    public void Evaluate_WithTwoMeaningfullyLowerNewObservations_ReturnsExplainedSuggestion()
    {
        GlobalRatingSuggestionEvaluation? result = this.policy.Evaluate(
            RatingValue.FromDouble(4.5d),
            RatingUpdatedAtUtc,
            new[]
            {
                Observation(3d, 4),
                Observation(3.5d, 3),
                Observation(5d, -2),
            },
            new GlobalRatingSuggestionCadence(true, null),
            RatingUpdatedAtUtc.AddDays(5));

        Assert.NotNull(result);
        Assert.Equal(GlobalRatingSuggestionReason.RecentExperiencesLower, result.Reason);
        Assert.Equal(2, result.NewObservationCount);
        Assert.Equal(2, result.RecentObservationCount);
        Assert.Equal(3d, result.LatestObservation.DoubleValue);
        Assert.Equal(3.25d, result.RecentAverage);
        Assert.Equal(3.5d, result.HistoricalMedian);
    }

    [Fact]
    public void Evaluate_WithOneNewObservation_DoesNotSuggest()
    {
        GlobalRatingSuggestionEvaluation? result = this.policy.Evaluate(
            RatingValue.FromDouble(5d),
            RatingUpdatedAtUtc,
            new[] { Observation(2d, 1), Observation(1d, -1) },
            new GlobalRatingSuggestionCadence(true, null),
            RatingUpdatedAtUtc.AddDays(2));

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_WithSmallDifference_DoesNotSuggest()
    {
        GlobalRatingSuggestionEvaluation? result = this.policy.Evaluate(
            RatingValue.FromDouble(4d),
            RatingUpdatedAtUtc,
            new[] { Observation(3.5d, 1), Observation(3.5d, 2) },
            new GlobalRatingSuggestionCadence(true, null),
            RatingUpdatedAtUtc.AddDays(3));

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_DuringPresentationCooldown_DoesNotRepeatSuggestion()
    {
        GlobalRatingSuggestionEvaluation? result = this.policy.Evaluate(
            RatingValue.FromDouble(2d),
            RatingUpdatedAtUtc,
            new[] { Observation(4d, 1), Observation(5d, 2) },
            new GlobalRatingSuggestionCadence(true, RatingUpdatedAtUtc.AddDays(2)),
            RatingUpdatedAtUtc.AddDays(31));

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_AfterCooldown_ReturnsHigherSuggestion()
    {
        GlobalRatingSuggestionEvaluation? result = this.policy.Evaluate(
            RatingValue.FromDouble(2d),
            RatingUpdatedAtUtc,
            new[] { Observation(4d, 1), Observation(5d, 2) },
            new GlobalRatingSuggestionCadence(true, RatingUpdatedAtUtc.AddDays(1)),
            RatingUpdatedAtUtc.AddDays(32));

        Assert.NotNull(result);
        Assert.Equal(GlobalRatingSuggestionReason.RecentExperiencesHigher, result.Reason);
    }

    [Fact]
    public void Evaluate_WhenDisabled_DoesNotSuggest()
    {
        GlobalRatingSuggestionEvaluation? result = this.policy.Evaluate(
            RatingValue.FromDouble(2d),
            RatingUpdatedAtUtc,
            new[] { Observation(4d, 1), Observation(5d, 2) },
            new GlobalRatingSuggestionCadence(false, null),
            RatingUpdatedAtUtc.AddDays(3));

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_WithNonUtcPresentationTimestamp_RejectsAmbiguousCadence()
    {
        DateTime localPresentation = new DateTime(
            2026,
            8,
            2,
            12,
            0,
            0,
            DateTimeKind.Local);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => this.policy.Evaluate(
            RatingValue.FromDouble(2d),
            RatingUpdatedAtUtc,
            new[] { Observation(4d, 1), Observation(5d, 2) },
            new GlobalRatingSuggestionCadence(true, localPresentation),
            RatingUpdatedAtUtc.AddDays(3)));

        Assert.Equal("LastPresentedAtUtc", exception.ParamName);
    }

    private static GlobalRatingSuggestionObservation Observation(double value, int dayOffset)
    {
        return new GlobalRatingSuggestionObservation(
            RatingValue.FromDouble(value),
            RatingUpdatedAtUtc.AddDays(dayOffset));
    }
}
