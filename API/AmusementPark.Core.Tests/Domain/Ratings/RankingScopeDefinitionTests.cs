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
            RankingPublicationMode.DurableSnapshot));
    }

    [Fact]
    public void ForParkItemCategory_WhenCategoryIsUnknown_ShouldRejectIt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankingFilterDefinition.ForParkItemCategory((ParkItemCategory)999));
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
            RankingPublicationMode.DurableSnapshot);
    }
}
