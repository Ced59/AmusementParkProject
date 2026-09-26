using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalCoverageTests
{
    [Fact]
    public void Constructor_WithNonUtcReviewTimestamp_Throws()
    {
        Assert.Throws<ArgumentException>(() => new HistoricalCoverage(
            1,
            1,
            0,
            0,
            new HistoricalFieldCoverage(1, 1),
            new HistoricalFieldCoverage(0, 0),
            DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local),
            HistoricalCoverageStatus.HighConfidence));
    }

    [Fact]
    public void Constructor_WithInconsistentPeriodCounts_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HistoricalCoverage(
            2,
            1,
            0,
            0,
            new HistoricalFieldCoverage(1, 2),
            new HistoricalFieldCoverage(0, 0),
            null,
            HistoricalCoverageStatus.Partial));
    }

    [Fact]
    public void Constructor_WithNameCoverageForAnotherPopulation_Throws()
    {
        Assert.Throws<ArgumentException>(() => new HistoricalCoverage(
            1,
            1,
            0,
            0,
            new HistoricalFieldCoverage(1, 2),
            new HistoricalFieldCoverage(0, 0),
            null,
            HistoricalCoverageStatus.Partial));
    }
}
