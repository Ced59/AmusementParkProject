using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkDetailSummaryReadRepositoryTests
{
    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.Cancelled)]
    public void BuildRatingSummary_WhenParkCannotReceiveRatings_ShouldExcludeRetainedAggregate(ParkStatus status)
    {
        RatingSummaryResult summary = ParkDetailSummaryReadRepository.BuildRatingSummary(
            "park-1",
            status,
            CreateAggregate());

        Assert.Equal(10, summary.RatingCount);
        Assert.Equal(RankingEvidenceLevel.Excluded, summary.Evidence?.Level);
        Assert.False(summary.Evidence?.IsEligibleForMainRanking);
        Assert.Equal(RankingIneligibilityReason.TargetUnavailable, summary.Evidence?.IneligibilityReason);
        Assert.Null(summary.Evidence?.NextThreshold);
    }

    [Fact]
    public void BuildRatingSummary_WhenParkCanReceiveRatings_ShouldEvaluateRetainedAggregate()
    {
        RatingSummaryResult summary = ParkDetailSummaryReadRepository.BuildRatingSummary(
            "park-1",
            ParkStatus.Operating,
            CreateAggregate());

        Assert.Equal(RankingEvidenceLevel.Eligible, summary.Evidence?.Level);
        Assert.True(summary.Evidence?.IsEligibleForMainRanking);
    }

    private static RatingAggregate CreateAggregate()
    {
        return new RatingAggregate
        {
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            RatingCount = 10,
            UniqueContributorCount = 10,
            RatingSum = 45,
            AverageRating = 4.5,
            BayesianScore = 4,
            MutationVersion = 1,
            CalculatedVersion = 1,
        };
    }
}
