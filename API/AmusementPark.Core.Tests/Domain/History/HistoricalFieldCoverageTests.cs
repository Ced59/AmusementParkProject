using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalFieldCoverageTests
{
    [Fact]
    public void Constructor_WithPartialCoverage_ComputesRoundedPercentage()
    {
        HistoricalFieldCoverage coverage = new HistoricalFieldCoverage(2, 3);

        Assert.Equal(66.7m, coverage.Percentage);
        Assert.False(coverage.IsComplete);
    }

    [Fact]
    public void Constructor_WithoutApplicableSubject_TreatsFieldAsComplete()
    {
        HistoricalFieldCoverage coverage = new HistoricalFieldCoverage(0, 0);

        Assert.Equal(100m, coverage.Percentage);
        Assert.True(coverage.IsComplete);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 1)]
    public void Constructor_WithInconsistentCounts_Throws(int documented, int applicable)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HistoricalFieldCoverage(documented, applicable));
    }
}
