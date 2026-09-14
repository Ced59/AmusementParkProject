using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitPreferenceCoverageSubscoreEvaluatorTests
{
    private readonly ParkFitPreferenceCoverageSubscoreEvaluator evaluator =
        new ParkFitPreferenceCoverageSubscoreEvaluator();

    [Fact]
    public void Evaluate_WhenHalfOfPreferencesArePresent_ShouldReturnFifty()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { ParkItemType.DarkRide, ParkItemType.RollerCoaster },
            new[] { BuildAttraction(ParkItemType.DarkRide) });

        Assert.Equal(ParkFitSubscoreState.Known, result.State);
        Assert.Equal(50m, result.Value);
        Assert.Equal(100m, result.CoveragePercent);
    }

    [Fact]
    public void Evaluate_WhenNoPreferenceIsSelected_ShouldReturnNeutralKnownValue()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            Array.Empty<ParkItemType>(),
            Array.Empty<ParkItem>());

        Assert.Equal(ParkFitSubscoreState.Known, result.State);
        Assert.Equal(100m, result.Value);
        Assert.Equal(100m, result.CoveragePercent);
    }

    [Fact]
    public void Evaluate_WhenSomeAttractionTypesAreUnknown_ShouldReduceCoverage()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { ParkItemType.DarkRide },
            new[]
            {
                BuildAttraction(ParkItemType.DarkRide),
                BuildAttraction(ParkItemType.Attraction),
            });

        Assert.Equal(100m, result.Value);
        Assert.Equal(50m, result.CoveragePercent);
        Assert.Contains(ParkFitSubscoreReasonCode.UnknownFactsExcluded, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenNoAttractionFactExists_ShouldReturnUnknown()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { ParkItemType.DarkRide },
            Array.Empty<ParkItem>());

        Assert.Equal(ParkFitSubscoreState.Unknown, result.State);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Evaluate_WhenATypeBelongsToAnotherCategory_ShouldKeepItUnknown()
    {
        ParkItem invalidItem = BuildAttraction(ParkItemType.DarkRide);
        invalidItem.Category = ParkItemCategory.Restaurant;

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { ParkItemType.DarkRide },
            new[] { invalidItem });

        Assert.Equal(ParkFitSubscoreState.Unknown, result.State);
    }

    [Theory]
    [InlineData(ParkItemType.Attraction)]
    [InlineData(ParkItemType.Restaurant)]
    [InlineData(ParkItemType.Other)]
    [InlineData((ParkItemType)99)]
    public void Evaluate_WhenPreferenceIsNotAPreciseAttractionType_ShouldRejectIt(
        ParkItemType type)
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new[] { type },
            Array.Empty<ParkItem>()));
    }

    private static ParkItem BuildAttraction(ParkItemType type)
    {
        return new ParkItem
        {
            Category = ParkItemCategory.Attraction,
            Type = type,
        };
    }
}
