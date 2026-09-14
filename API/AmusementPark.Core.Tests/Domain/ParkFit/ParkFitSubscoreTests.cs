using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitSubscoreTests
{
    [Fact]
    public void Constructor_WhenKnown_ShouldPreserveAndOrderReasons()
    {
        ParkFitSubscore result = new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
            ParkFitSubscoreState.Known,
            75m,
            80m,
            ParkFitDataConfidence.Medium,
            new[]
            {
                ParkFitSubscoreReasonCode.MinimumMemberBoundApplied,
                ParkFitSubscoreReasonCode.KnownFactsNormalized,
                ParkFitSubscoreReasonCode.KnownFactsNormalized,
            });

        Assert.Equal(75m, result.Value);
        Assert.Equal(80m, result.CoveragePercent);
        Assert.Equal(
            new[]
            {
                ParkFitSubscoreReasonCode.KnownFactsNormalized,
                ParkFitSubscoreReasonCode.MinimumMemberBoundApplied,
            },
            result.Reasons);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_WhenCoverageIsOutsideRange_ShouldRejectIt(decimal coveragePercent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
            ParkFitSubscoreState.Unknown,
            null,
            coveragePercent,
            ParkFitDataConfidence.Unknown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_WhenKnownValueIsInvalid_ShouldRejectIt(int? value)
    {
        decimal? decimalValue = value;

        Assert.Throws<ArgumentOutOfRangeException>(() => new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
            ParkFitSubscoreState.Known,
            decimalValue,
            100m,
            ParkFitDataConfidence.High));
    }

    [Fact]
    public void Constructor_WhenKnownCoverageIsZero_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
            ParkFitSubscoreState.Known,
            50m,
            0m,
            ParkFitDataConfidence.High));
    }

    [Theory]
    [InlineData(ParkFitSubscoreState.Unknown)]
    [InlineData(ParkFitSubscoreState.NotApplicable)]
    public void Constructor_WhenUnavailableStateCarriesValue_ShouldRejectIt(
        ParkFitSubscoreState state)
    {
        Assert.Throws<ArgumentException>(() => new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
            state,
            50m,
            0m,
            ParkFitDataConfidence.Unknown));
    }

    [Fact]
    public void Constructor_WhenNotApplicableCarriesCoverage_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new ParkFitSubscore(
            ParkFitSubscoreKind.TravelConvenience,
            ParkFitSubscoreState.NotApplicable,
            null,
            10m,
            ParkFitDataConfidence.Unknown));
    }

    [Theory]
    [InlineData(99, 0, 0)]
    [InlineData(0, 99, 0)]
    [InlineData(0, 0, 99)]
    public void Constructor_WhenEnumIsInvalid_ShouldRejectIt(
        int kind,
        int state,
        int confidence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParkFitSubscore(
            (ParkFitSubscoreKind)kind,
            (ParkFitSubscoreState)state,
            null,
            0m,
            (ParkFitDataConfidence)confidence));
    }

    [Fact]
    public void Constructor_WhenReasonIsInvalid_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
            ParkFitSubscoreState.Unknown,
            null,
            0m,
            ParkFitDataConfidence.Unknown,
            new[] { (ParkFitSubscoreReasonCode)99 }));
    }
}
