using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RankingScopeDefinitionTests
{
    [Fact]
    public void Constructor_WhenGlobalParkScopeIsValid_ShouldPreserveItsPublicationPolicy()
    {
        RankingScopeDefinition definition = CreateGlobalParkDefinition();

        Assert.Equal("parks:global", definition.Key.Value);
        Assert.Equal(RankingTargetFamily.Parks, definition.TargetFamily);
        Assert.Equal(RankingScopeFilterKind.Global, definition.Filter.Kind);
        Assert.Null(definition.Filter.ParkItemCategory);
        Assert.True(definition.IsPublic);
        Assert.Equal(RankingEligibilityPolicy.InitialMethodologyVersion, definition.MethodologyVersion);
        Assert.Equal(3, definition.MinimumEligibleEntries);
        Assert.Equal(500, definition.PageSize);
        Assert.Equal(0.0001m, definition.ScoreTieEpsilon);
        Assert.Equal(RankingPublicationMode.DurableSnapshot, definition.PublicationMode);
    }

    [Fact]
    public void Constructor_WhenParkItemCategoryScopeIsValid_ShouldPreserveItsTypedFilter()
    {
        RankingScopeDefinition definition = new RankingScopeDefinition(
            RankingScopeKey.Parse("park-items:category:show"),
            RankingTargetFamily.ParkItems,
            RankingFilterDefinition.ForParkItemCategory(ParkItemCategory.Show),
            isPublic: false,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 4,
            pageSize: 250,
            scoreTieEpsilon: 0.0001m,
            RankingPublicationMode.DurableSnapshot);

        Assert.Equal(RankingScopeFilterKind.ParkItemCategory, definition.Filter.Kind);
        Assert.Equal(ParkItemCategory.Show, definition.Filter.ParkItemCategory);
        Assert.False(definition.IsPublic);
    }

    [Theory]
    [InlineData(RankingTargetFamily.Parks, ParkItemCategory.Attraction)]
    [InlineData(RankingTargetFamily.Parks, ParkItemCategory.Show)]
    public void Constructor_WhenParkScopeUsesAnItemFilter_ShouldRejectIt(
        RankingTargetFamily targetFamily,
        ParkItemCategory category)
    {
        Assert.Throws<ArgumentException>(() => new RankingScopeDefinition(
            RankingScopeKey.Parse("parks:global"),
            targetFamily,
            RankingFilterDefinition.ForParkItemCategory(category),
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 3,
            pageSize: 500,
            scoreTieEpsilon: 0.0001m,
            RankingPublicationMode.DurableSnapshot));
    }

    [Fact]
    public void Constructor_WhenParkItemScopeUsesTheGlobalFilter_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new RankingScopeDefinition(
            RankingScopeKey.Parse("park-items:category:attraction"),
            RankingTargetFamily.ParkItems,
            RankingFilterDefinition.Global,
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 3,
            pageSize: 500,
            scoreTieEpsilon: 0.0001m,
            RankingPublicationMode.DurableSnapshot));
    }

    [Fact]
    public void Constructor_WhenKeyDoesNotMatchTheTypedFilter_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new RankingScopeDefinition(
            RankingScopeKey.Parse("park-items:category:hotel"),
            RankingTargetFamily.ParkItems,
            RankingFilterDefinition.ForParkItemCategory(ParkItemCategory.Restaurant),
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 3,
            pageSize: 500,
            scoreTieEpsilon: 0.0001m,
            RankingPublicationMode.DurableSnapshot));
    }

    [Theory]
    [InlineData(0, 500)]
    [InlineData(-1, 500)]
    [InlineData(3, 249)]
    [InlineData(3, 501)]
    public void Constructor_WhenThresholdOrPageSizeIsOutsideThePolicy_ShouldRejectIt(
        int minimumEligibleEntries,
        int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RankingScopeDefinition(
            RankingScopeKey.Parse("parks:global"),
            RankingTargetFamily.Parks,
            RankingFilterDefinition.Global,
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries,
            pageSize,
            scoreTieEpsilon: 0.0001m,
            RankingPublicationMode.DurableSnapshot));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.0001d)]
    [InlineData(0.1001d)]
    public void Constructor_WhenScoreTieEpsilonIsOutsideThePolicy_ShouldRejectIt(
        double scoreTieEpsilon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RankingScopeDefinition(
            RankingScopeKey.Parse("parks:global"),
            RankingTargetFamily.Parks,
            RankingFilterDefinition.Global,
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 3,
            pageSize: 500,
            scoreTieEpsilon: (decimal)scoreTieEpsilon,
            RankingPublicationMode.DurableSnapshot));
    }

    [Fact]
    public void ForParkItemCategory_WhenCategoryIsUnknown_ShouldRejectIt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankingFilterDefinition.ForParkItemCategory((ParkItemCategory)999));
    }

    [Theory]
    [InlineData(2, false, RankingIneligibilityReason.TooFewComparableEntries)]
    [InlineData(3, true, null)]
    [InlineData(4, true, null)]
    public void EvaluatePublication_ShouldApplyTheScopeMinimum(
        int eligibleEntryCount,
        bool expectedEligibility,
        RankingIneligibilityReason? expectedReason)
    {
        RankingPublicationEligibility result = CreateGlobalParkDefinition()
            .EvaluatePublication(eligibleEntryCount);

        Assert.Equal(expectedEligibility, result.IsEligible);
        Assert.Equal(expectedReason, result.IneligibilityReason);
    }

    [Fact]
    public void EvaluatePublication_WhenCountIsNegative_ShouldRejectIt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateGlobalParkDefinition().EvaluatePublication(-1));
    }

    [Theory]
    [InlineData(4.5d, 4.49995d, true)]
    [InlineData(4.5d, 4.4998d, false)]
    public void AreScoresTied_ShouldUseTheScopeMethodologyEpsilon(
        double leftScore,
        double rightScore,
        bool expected)
    {
        Assert.Equal(expected, CreateGlobalParkDefinition().AreScoresTied(leftScore, rightScore));
    }

    [Fact]
    public void AcceptsTarget_ShouldApplyTheTypedScopeFilter()
    {
        RankingScopeDefinition definition = new RankingScopeDefinition(
            RankingScopeKey.Parse("park-items:category:attraction"),
            RankingTargetFamily.ParkItems,
            RankingFilterDefinition.ForParkItemCategory(ParkItemCategory.Attraction),
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 3,
            pageSize: 500,
            scoreTieEpsilon: 0.0001m,
            RankingPublicationMode.DurableSnapshot);

        Assert.True(definition.AcceptsTarget(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction));
        Assert.False(definition.AcceptsTarget(
            RatingTargetType.ParkItem,
            ParkItemCategory.Restaurant));
        Assert.False(definition.AcceptsTarget(RatingTargetType.Park, null));
    }

    private static RankingScopeDefinition CreateGlobalParkDefinition()
    {
        return new RankingScopeDefinition(
            RankingScopeKey.Parse("parks:global"),
            RankingTargetFamily.Parks,
            RankingFilterDefinition.Global,
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 3,
            pageSize: 500,
            scoreTieEpsilon: 0.0001m,
            RankingPublicationMode.DurableSnapshot);
    }
}
