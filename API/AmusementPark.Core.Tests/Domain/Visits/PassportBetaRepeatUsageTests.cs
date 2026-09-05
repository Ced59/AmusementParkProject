using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportBetaRepeatUsageTests
{
    [Theory]
    [InlineData(0, 0, 0, PassportBetaRepeatUsageSignal.NotObserved)]
    [InlineData(8, 1, 12.5, PassportBetaRepeatUsageSignal.Emerging)]
    [InlineData(8, 2, 25, PassportBetaRepeatUsageSignal.Emerging)]
    [InlineData(8, 3, 37.5, PassportBetaRepeatUsageSignal.Candidate)]
    public void FromCounts_ShouldCalculateRateAndSignal(
        long completedUsers,
        long returningUsers,
        decimal expectedRate,
        PassportBetaRepeatUsageSignal expectedSignal)
    {
        PassportBetaRepeatUsage result = PassportBetaRepeatUsage.FromCounts(
            completedUsers,
            returningUsers);

        Assert.Equal(expectedRate, result.RatePercent);
        Assert.Equal(expectedSignal, result.Signal);
        Assert.True(result.RequiresQualitativeValidation);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 2)]
    public void FromCounts_WithInvalidCohort_ShouldRejectIt(
        long completedUsers,
        long returningUsers)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PassportBetaRepeatUsage.FromCounts(
            completedUsers,
            returningUsers));
    }
}
