using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RatingMethodologyCatalogTests
{
    [Fact]
    public void Current_ShouldDescribeTheAuthoritativeInitialMethodology()
    {
        RatingMethodologyDefinition definition = RatingMethodologyCatalog.Current;

        Assert.Equal(RankingEligibilityPolicy.InitialMethodologyVersion, definition.Version);
        Assert.Equal(new DateOnly(2026, 8, 31), definition.EffectiveDate);
        Assert.Null(definition.PreviousVersion);
        Assert.Equal(0.5m, definition.RatingMinimum);
        Assert.Equal(5m, definition.RatingMaximum);
        Assert.Equal(0.5m, definition.RatingStep);
        Assert.Equal(3.5d, definition.BayesianPriorMean);
        Assert.Equal(10, definition.BayesianPriorWeight);
        Assert.Equal(0.7d, definition.ParkDirectScoreWeight);
        Assert.Equal(0.3d, definition.ParkItemsScoreWeight);
        Assert.True(definition.BalancesItemCategoriesEqually);
        Assert.Equal("competition", definition.RankingConvention);
    }

    [Fact]
    public void TryResolve_ShouldOnlyExposePublishedVersions()
    {
        bool found = RatingMethodologyCatalog.TryResolve(
            RankingEligibilityPolicy.InitialMethodologyVersion,
            out RatingMethodologyDefinition? definition);
        bool missing = RatingMethodologyCatalog.TryResolve(
            RatingMethodologyVersion.Parse("ratings-2099-01"),
            out RatingMethodologyDefinition? missingDefinition);

        Assert.True(found);
        Assert.Same(RatingMethodologyCatalog.Current, definition);
        Assert.False(missing);
        Assert.Null(missingDefinition);
        Assert.Single(RatingMethodologyCatalog.All);
    }
}
