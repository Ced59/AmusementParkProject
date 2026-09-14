using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitHardFilterEvaluationTests
{
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 9, 14);

    [Fact]
    public void Constructor_WhenSeveralFiltersAreUnknown_ShouldPreserveTheirCount()
    {
        ParkFitHardFilterEvaluation result = new ParkFitHardFilterEvaluation(
            EvaluationDate,
            3,
            0,
            2);

        Assert.Equal(ParkFitHardFilterState.Unknown, result.State);
        Assert.Equal(3, result.EvaluatedFilterCount);
        Assert.Equal(2, result.UnknownFilterCount);
        Assert.Equal(EvaluationDate, result.EvaluationDate);
    }

    [Fact]
    public void Constructor_WhenAtLeastOneFilterFails_ShouldPreferFailedState()
    {
        ParkFitHardFilterEvaluation result = new ParkFitHardFilterEvaluation(
            EvaluationDate,
            3,
            1,
            1);

        Assert.Equal(ParkFitHardFilterState.Failed, result.State);
        Assert.Equal(1, result.FailedFilterCount);
        Assert.Equal(1, result.UnknownFilterCount);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 2, 0)]
    [InlineData(1, 0, -1)]
    [InlineData(1, 1, 1)]
    public void Constructor_WhenCountsAreInconsistent_ShouldRejectThem(
        int evaluatedFilterCount,
        int failedFilterCount,
        int unknownFilterCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ParkFitHardFilterEvaluation(
                EvaluationDate,
                evaluatedFilterCount,
                failedFilterCount,
                unknownFilterCount));
    }
}
