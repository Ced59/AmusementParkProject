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
            aggregate,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: null);

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
            aggregate,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: null);

        Assert.Null(result.Evidence);
    }

    [Fact]
    public void CreateSummary_WhenAggregateCalculationIsCurrent_ShouldEvaluateEvidence()
    {
        RatingAggregate aggregate = CreateAggregate(mutationVersion: 3, calculatedVersion: 3);

        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: null);

        Assert.Equal(RankingEvidenceLevel.Eligible, result.Evidence?.Level);
    }

    [Fact]
    public void CreateSummary_WhenCalculatedVersionIsAhead_ShouldExposeIntegrityExclusion()
    {
        RatingAggregate aggregate = CreateAggregate(mutationVersion: 2, calculatedVersion: 3);

        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: null);

        Assert.Equal(RankingEvidenceLevel.Excluded, result.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            result.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void CreateSummary_WhenMissingAggregateIsKnownInvalid_ShouldExposeIntegrityExclusion()
    {
        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate: null,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: false);

        Assert.Equal(RankingEvidenceLevel.Excluded, result.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            result.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void CreateSummary_WhenMissingAggregateIntegrityIsUnknown_ShouldWithholdEvidence()
    {
        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate: null,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: null);

        Assert.Null(result.Evidence);
    }

    [Fact]
    public void CreateSummary_WhenObservationsContainDuplicateContributors_ShouldUseDistinctCount()
    {
        RatingAggregate aggregate = CreateAggregate(mutationVersion: 3, calculatedVersion: 3);
        aggregate.RatingCount = 10;
        aggregate.UniqueContributorCount = 5;

        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: null);

        Assert.Equal(5, result.UniqueContributorCount);
        Assert.Equal(10, result.RatingObservationCount);
        Assert.Equal(RankingEvidenceLevel.Excluded, result.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            result.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void CreateSummary_WhenContributorsExceedObservations_ShouldWithholdMalformedEvidence()
    {
        RatingAggregate aggregate = CreateAggregate(mutationVersion: 3, calculatedVersion: 3);
        aggregate.RatingCount = 5;
        aggregate.UniqueContributorCount = 10;

        RatingSummaryResult result = RatingResultFactory.CreateSummary(
            RatingTargetType.Park,
            "park-1",
            aggregate,
            targetCanReceiveVisitorRatings: true,
            aggregateIntegrityIsValid: null);

        Assert.Equal(5, result.RatingObservationCount);
        Assert.Null(result.Evidence);
        Assert.Null(result.UniqueContributorCount);
    }

    private static RatingAggregate CreateAggregate(long? mutationVersion, long? calculatedVersion)
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
            MutationVersion = mutationVersion,
            CalculatedVersion = calculatedVersion,
        };
    }
}
