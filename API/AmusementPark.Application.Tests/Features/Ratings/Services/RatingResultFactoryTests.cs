using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingResultFactoryTests
{
    [Fact]
    public void CreateSummary_WhenAggregateCalculationIsStale_ShouldExposeIntegrityExclusion()
    {
        RatingAggregate aggregate = CreateAggregate(mutationVersion: 3, calculatedVersion: 2);

        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate);

        Assert.Equal(RankingEvidenceLevel.Excluded, result.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            result.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void CreateSummary_WhenAggregateCalculationVersionIsUnknown_ShouldWithholdEvidence()
    {
        RatingAggregate aggregate = CreateAggregate(mutationVersion: null, calculatedVersion: null);

        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate);

        Assert.Null(result.Evidence);
    }

    [Fact]
    public void CreateSummary_WhenAggregateCalculationIsCurrent_ShouldEvaluateEvidence()
    {
        RatingAggregate aggregate = CreateAggregate(mutationVersion: 3, calculatedVersion: 3);

        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate);

        Assert.Equal(RankingEvidenceLevel.Eligible, result.Evidence?.Level);
    }

    private static RatingAggregate CreateAggregate(long? mutationVersion, long? calculatedVersion)
    {
        return new RatingAggregate
        {
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            RatingCount = 10,
            RatingSum = 45,
            AverageRating = 4.5,
            BayesianScore = 4,
            MutationVersion = mutationVersion,
            CalculatedVersion = calculatedVersion,
        };
    }
}
