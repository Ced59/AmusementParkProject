using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class CompetitionRankCalculatorTests
{
    [Fact]
    public void AssignOrderedRanks_WhenScoresTie_ShouldSkipTheFollowingCompetitionRank()
    {
        RankingScopeDefinition scope = CreateScope(0.0001m);

        IReadOnlyList<CompetitionRankAssignment> result =
            CompetitionRankCalculator.AssignOrderedRanks(
                scope,
                new[] { 4.5d, 4.49995d, 4.4d, 4.3d });

        Assert.Equal(new[] { 1, 1, 3, 4 }, result.Select(static item => item.Rank));
        Assert.Equal(new[] { 1, 2, 3, 4 }, result.Select(static item => item.Position));
    }

    [Fact]
    public void AssignOrderedRanks_WhenDifferenceEqualsTieEpsilon_ShouldUseDistinctRanks()
    {
        RankingScopeDefinition scope = CreateScope(0.0001m);

        IReadOnlyList<CompetitionRankAssignment> result =
            CompetitionRankCalculator.AssignOrderedRanks(
                scope,
                new[] { 4.5d, 4.4999d });

        Assert.Equal(new[] { 1, 2 }, result.Select(static item => item.Rank));
    }

    [Fact]
    public void AssignOrderedRanks_WhenScoreIsNotFinite_ShouldRejectIt()
    {
        RankingScopeDefinition scope = CreateScope(0.0001m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CompetitionRankCalculator.AssignOrderedRanks(scope, new[] { double.NaN }));
    }

    private static RankingScopeDefinition CreateScope(decimal tieEpsilon)
    {
        return new RankingScopeDefinition(
            RankingScopeKey.Parse("parks:global"),
            RankingTargetFamily.Parks,
            RankingFilterDefinition.Global,
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            minimumEligibleEntries: 3,
            pageSize: 250,
            scoreTieEpsilon: tieEpsilon,
            publicationMode: RankingPublicationMode.DurableSnapshot);
    }
}
