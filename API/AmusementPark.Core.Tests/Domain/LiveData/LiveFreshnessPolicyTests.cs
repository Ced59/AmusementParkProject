using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveFreshnessPolicyTests
{
    private static readonly DateTime NowUtc = new(
        2026,
        9,
        28,
        12,
        0,
        0,
        DateTimeKind.Utc);

    [Theory]
    [InlineData(0, LiveFreshnessState.Fresh, LiveFreshnessReason.WithinFreshWindow, true)]
    [InlineData(5, LiveFreshnessState.Fresh, LiveFreshnessReason.WithinFreshWindow, true)]
    [InlineData(6, LiveFreshnessState.Aging, LiveFreshnessReason.WithinAgingWindow, true)]
    [InlineData(10, LiveFreshnessState.Aging, LiveFreshnessReason.WithinAgingWindow, true)]
    [InlineData(11, LiveFreshnessState.Stale, LiveFreshnessReason.WithinStaleWindow, true)]
    [InlineData(15, LiveFreshnessState.Stale, LiveFreshnessReason.WithinStaleWindow, true)]
    [InlineData(16, LiveFreshnessState.Expired, LiveFreshnessReason.ObservationExpired, false)]
    public void Assess_ShouldApplyInclusiveFreshnessBoundaries(
        int ageInMinutes,
        LiveFreshnessState expectedState,
        LiveFreshnessReason expectedReason,
        bool canBePresentedAsCurrent)
    {
        LiveFreshnessPolicy policy = CreatePolicy();
        DateTime observedAtUtc = NowUtc.AddMinutes(-ageInMinutes);

        LiveFreshnessAssessment assessment = policy.Assess(observedAtUtc, NowUtc);

        Assert.Equal(expectedState, assessment.State);
        Assert.Equal(expectedReason, assessment.Reason);
        Assert.Equal(TimeSpan.FromMinutes(ageInMinutes), assessment.Age);
        Assert.Equal(observedAtUtc.AddMinutes(15), assessment.ExpiresAtUtc);
        Assert.Equal(canBePresentedAsCurrent, assessment.CanBePresentedAsCurrent);
    }

    [Fact]
    public void Assess_WhenSourceTimestampIsMissing_ShouldReturnUnavailable()
    {
        LiveFreshnessAssessment assessment = CreatePolicy().Assess(null, NowUtc);

        Assert.Equal(LiveFreshnessState.Unavailable, assessment.State);
        Assert.Equal(LiveFreshnessReason.MissingSourceTimestamp, assessment.Reason);
        Assert.Null(assessment.Age);
        Assert.Null(assessment.ExpiresAtUtc);
        Assert.False(assessment.CanBePresentedAsCurrent);
    }

    [Fact]
    public void Assess_WhenTimestampIsWithinAcceptedFutureSkew_ShouldClampAgeToZero()
    {
        DateTime observedAtUtc = NowUtc.AddSeconds(30);

        LiveFreshnessAssessment assessment = CreatePolicy().Assess(observedAtUtc, NowUtc);

        Assert.Equal(LiveFreshnessState.Fresh, assessment.State);
        Assert.Equal(TimeSpan.Zero, assessment.Age);
    }

    [Fact]
    public void Assess_WhenTimestampIsTooFarInFuture_ShouldReturnUnavailable()
    {
        LiveFreshnessAssessment assessment = CreatePolicy().Assess(
            NowUtc.AddMinutes(2),
            NowUtc);

        Assert.Equal(LiveFreshnessState.Unavailable, assessment.State);
        Assert.Equal(LiveFreshnessReason.SourceTimestampTooFarInFuture, assessment.Reason);
        Assert.False(assessment.CanBePresentedAsCurrent);
    }

    [Fact]
    public void Constructor_WhenThresholdsAreNotStrictlyOrdered_ShouldRejectPolicy()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(1)));

        Assert.Equal(LiveDataErrorCodes.InvalidFreshnessThresholds, exception.Code);
    }

    [Fact]
    public void Assess_WhenNowIsNotUtc_ShouldRejectTimestamp()
    {
        DateTime localNow = DateTime.SpecifyKind(NowUtc, DateTimeKind.Local);

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreatePolicy().Assess(NowUtc, localNow));

        Assert.Equal(LiveDataErrorCodes.InvalidTimestamp, exception.Code);
    }

    [Fact]
    public void Assess_WhenExpirationCannotBeRepresented_ShouldRejectTimestamp()
    {
        DateTime nearMaximumUtc = DateTime.SpecifyKind(
            DateTime.MaxValue.AddMinutes(-1),
            DateTimeKind.Utc);

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreatePolicy().Assess(nearMaximumUtc, nearMaximumUtc));

        Assert.Equal(LiveDataErrorCodes.InvalidTimestamp, exception.Code);
    }

    private static LiveFreshnessPolicy CreatePolicy()
    {
        return new LiveFreshnessPolicy(
            "freshness-1",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(1));
    }
}
