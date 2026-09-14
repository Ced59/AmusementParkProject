using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitIndoorResilienceSubscoreEvaluatorTests
{
    private readonly ParkFitIndoorResilienceSubscoreEvaluator evaluator =
        new ParkFitIndoorResilienceSubscoreEvaluator();

    [Fact]
    public void Evaluate_WhenPreferenceIsDisabled_ShouldReturnNotApplicable()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            false,
            Array.Empty<ParkItem>());

        Assert.Equal(ParkFitSubscoreState.NotApplicable, result.State);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Evaluate_WhenHalfOfKnownAttractionsAreIndoor_ShouldReturnFifty()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            true,
            new[] { BuildAttraction(true), BuildAttraction(false) });

        Assert.Equal(ParkFitSubscoreState.Known, result.State);
        Assert.Equal(50m, result.Value);
        Assert.Equal(100m, result.CoveragePercent);
    }

    [Fact]
    public void Evaluate_WhenAClassificationIsMissing_ShouldKeepItUnknownInCoverage()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            true,
            new[] { BuildAttraction(true), BuildAttraction(null) });

        Assert.Equal(100m, result.Value);
        Assert.Equal(50m, result.CoveragePercent);
        Assert.Contains(ParkFitSubscoreReasonCode.UnknownFactsExcluded, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenEveryClassificationIsMissing_ShouldReturnUnknown()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            true,
            new[] { BuildAttraction(null) });

        Assert.Equal(ParkFitSubscoreState.Unknown, result.State);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Evaluate_WhenAnItemIsNotAnAttraction_ShouldRejectIt()
    {
        ParkItem invalidItem = BuildAttraction(true);
        invalidItem.Category = ParkItemCategory.Restaurant;

        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            true,
            new[] { invalidItem }));
    }

    private static ParkItem BuildAttraction(bool? isIndoor)
    {
        return new ParkItem
        {
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.FamilyRide,
            AttractionDetails = new AttractionDetails
            {
                IsIndoor = isIndoor,
            },
        };
    }
}
