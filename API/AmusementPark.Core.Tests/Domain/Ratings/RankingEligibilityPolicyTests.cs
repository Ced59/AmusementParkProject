using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RankingEligibilityPolicyTests
{
    public static TheoryData<int, RankingEvidenceLevel, bool, RankingIneligibilityReason?> EvidenceBoundaries => new TheoryData<int, RankingEvidenceLevel, bool, RankingIneligibilityReason?>
    {
        { 0, RankingEvidenceLevel.NoEvidence, false, RankingIneligibilityReason.NoRatings },
        { 1, RankingEvidenceLevel.Insufficient, false, RankingIneligibilityReason.TooFewUniqueContributors },
        { 2, RankingEvidenceLevel.Insufficient, false, RankingIneligibilityReason.TooFewUniqueContributors },
        { 3, RankingEvidenceLevel.Provisional, false, RankingIneligibilityReason.TooFewUniqueContributors },
        { 9, RankingEvidenceLevel.Provisional, false, RankingIneligibilityReason.TooFewUniqueContributors },
        { 10, RankingEvidenceLevel.Eligible, true, null },
        { 29, RankingEvidenceLevel.Eligible, true, null },
        { 30, RankingEvidenceLevel.Established, true, null },
        { 99, RankingEvidenceLevel.Established, true, null },
        { 100, RankingEvidenceLevel.StrongEvidence, true, null },
    };

    [Theory]
    [MemberData(nameof(EvidenceBoundaries))]
    public void EvaluateSimpleTarget_AtEachBoundary_ShouldReturnExpectedEvidence(
        int contributorCount,
        RankingEvidenceLevel expectedLevel,
        bool expectedEligibility,
        RankingIneligibilityReason? expectedReason)
    {
        RankingEligibilityPolicy policy = RankingEligibilityPolicy.Initial;
        SimpleRankingEvidenceInput input = CreateSimpleInput(contributorCount, contributorCount);

        RankingEvidence evidence = policy.EvaluateSimpleTarget(input);

        Assert.Equal(expectedLevel, evidence.Level);
        Assert.Equal(expectedEligibility, evidence.IsEligibleForMainRanking);
        Assert.Equal(expectedReason, evidence.IneligibilityReason);
        Assert.Equal(contributorCount, evidence.UniqueContributorCount);
        Assert.Equal(contributorCount, evidence.RatingObservationCount);
        Assert.Equal(policy.Version, evidence.MethodologyVersion);
        Assert.Null(evidence.DirectParkContributorCount);
        Assert.Null(evidence.ItemContributorCount);
    }

    [Theory]
    [InlineData(false, false, true, RankingIneligibilityReason.TargetUnavailable)]
    [InlineData(true, true, true, RankingIneligibilityReason.TargetExcluded)]
    [InlineData(true, false, false, RankingIneligibilityReason.AggregateIntegrityFailure)]
    public void EvaluateSimpleTarget_WhenTargetCannotBeRanked_ShouldReturnExcluded(
        bool canReceiveRatings,
        bool isExcluded,
        bool aggregateIntegrityIsValid,
        RankingIneligibilityReason expectedReason)
    {
        SimpleRankingEvidenceInput input = new SimpleRankingEvidenceInput(
            100,
            100,
            canReceiveRatings,
            isExcluded,
            aggregateIntegrityIsValid);

        RankingEvidence evidence = RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(input);

        Assert.Equal(RankingEvidenceLevel.Excluded, evidence.Level);
        Assert.False(evidence.IsEligibleForMainRanking);
        Assert.Equal(expectedReason, evidence.IneligibilityReason);
    }

    [Fact]
    public void EvaluateSimpleTarget_WhenObservationsExceedContributors_ShouldRejectDuplicateOrTemporalInput()
    {
        SimpleRankingEvidenceInput input = CreateSimpleInput(10, 16);

        Assert.Throws<ArgumentException>(
            () => RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(input));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    [InlineData(2, 1)]
    public void EvaluateSimpleTarget_WhenCountsAreIncoherent_ShouldRejectInput(
        int contributorCount,
        int observationCount)
    {
        SimpleRankingEvidenceInput input = CreateSimpleInput(contributorCount, observationCount);

        Assert.ThrowsAny<ArgumentException>(
            () => RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(input));
    }

    [Fact]
    public void EvaluateParkItemComponent_WhenCoverageMeetsAllThresholds_ShouldBeEligible()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 14,
            directContributorCount: 10,
            itemContributorCount: 12,
            categories: new[]
            {
                new RankingCategoryCoverage(4, 3),
                new RankingCategoryCoverage(5, 2),
            });

        ParkItemComponentEligibility eligibility = RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(input);

        Assert.True(eligibility.IsEligible);
        Assert.Equal(5, eligibility.EligibleItemCount);
        Assert.Equal(2, eligibility.EligibleCategoryCount);
        Assert.Null(eligibility.IneligibilityReason);
    }

    [Fact]
    public void EvaluateParkItemComponent_WhenCategoryHasOnlyOnePublicItem_ShouldCountItsEligibleItemAsCoverage()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 15,
            directContributorCount: 10,
            itemContributorCount: 10,
            categories: new[]
            {
                new RankingCategoryCoverage(1, 1),
                new RankingCategoryCoverage(6, 4),
            });

        ParkItemComponentEligibility eligibility = RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(input);

        Assert.True(eligibility.IsEligible);
        Assert.Equal(2, eligibility.EligibleCategoryCount);
    }

    [Fact]
    public void EvaluateParkItemComponent_WhenExplicitSingleCategoryParkMeetsItemThreshold_ShouldBeEligible()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 12,
            directContributorCount: 10,
            itemContributorCount: 10,
            categories: new[] { new RankingCategoryCoverage(7, 5) },
            isSingleCategoryParkException: true);

        ParkItemComponentEligibility eligibility = RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(input);

        Assert.True(eligibility.IsEligible);
        Assert.Equal(1, eligibility.EligibleCategoryCount);
    }

    [Fact]
    public void EvaluateParkItemComponent_WhenSingleCategoryIsNotAnExplicitException_ShouldRejectCoverage()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 12,
            directContributorCount: 10,
            itemContributorCount: 10,
            categories: new[] { new RankingCategoryCoverage(7, 5) });

        ParkItemComponentEligibility eligibility = RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(input);

        Assert.False(eligibility.IsEligible);
        Assert.Equal(RankingIneligibilityReason.InsufficientCategoryCoverage, eligibility.IneligibilityReason);
    }

    [Theory]
    [InlineData(9, 0, 0, RankingIneligibilityReason.TooFewUniqueContributors)]
    [InlineData(10, 4, 2, RankingIneligibilityReason.InsufficientItemCoverage)]
    [InlineData(10, 5, 1, RankingIneligibilityReason.InsufficientCategoryCoverage)]
    public void EvaluateParkItemComponent_WhenOneThresholdIsMissing_ShouldReturnPreciseReason(
        int itemContributorCount,
        int eligibleItemCount,
        int coveredCategoryCount,
        RankingIneligibilityReason expectedReason)
    {
        IReadOnlyCollection<RankingCategoryCoverage> categories = coveredCategoryCount == 2
            ? new[]
            {
                new RankingCategoryCoverage(3, Math.Min(3, eligibleItemCount)),
                new RankingCategoryCoverage(3, Math.Max(0, eligibleItemCount - 3)),
            }
            : new[]
            {
                new RankingCategoryCoverage(Math.Max(1, eligibleItemCount), eligibleItemCount),
                new RankingCategoryCoverage(3, 0),
            };
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 10,
            directContributorCount: 10,
            itemContributorCount: itemContributorCount,
            categories: categories);

        ParkItemComponentEligibility eligibility = RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(input);

        Assert.False(eligibility.IsEligible);
        Assert.Equal(expectedReason, eligibility.IneligibilityReason);
    }

    [Fact]
    public void EvaluateParkItemComponent_WhenCategoryOrderChanges_ShouldKeepTheSameVerdict()
    {
        RankingCategoryCoverage first = new RankingCategoryCoverage(4, 3);
        RankingCategoryCoverage second = new RankingCategoryCoverage(5, 2);
        ParkRankingEvidenceInput forward = CreateParkInput(14, 10, 12, new[] { first, second });
        ParkRankingEvidenceInput reverse = CreateParkInput(14, 10, 12, new[] { second, first });

        ParkItemComponentEligibility forwardResult = RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(forward);
        ParkItemComponentEligibility reverseResult = RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(reverse);

        Assert.Equal(forwardResult, reverseResult);
    }

    [Fact]
    public void EvaluatePark_WhenDirectComponentIsBelowThreshold_ShouldRemainProvisional()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 19,
            directContributorCount: 9,
            itemContributorCount: 10,
            categories: new[]
            {
                new RankingCategoryCoverage(3, 3),
                new RankingCategoryCoverage(2, 2),
            });

        RankingEvidence evidence = RankingEligibilityPolicy.Initial.EvaluatePark(input);

        Assert.Equal(RankingEvidenceLevel.Provisional, evidence.Level);
        Assert.False(evidence.IsEligibleForMainRanking);
        Assert.Equal(RankingIneligibilityReason.TooFewUniqueContributors, evidence.IneligibilityReason);
        Assert.Equal(10, evidence.UniqueContributorCount);
        Assert.Equal(50, evidence.RatingObservationCount);
        Assert.Equal(9, evidence.DirectParkContributorCount);
        Assert.Equal(10, evidence.ItemContributorCount);
        Assert.Equal(5, evidence.EligibleItemCount);
        Assert.Equal(2, evidence.EligibleCategoryCount);
    }

    [Fact]
    public void EvaluatePark_WhenDirectComponentIsEligible_ShouldNotRequireItemCoverage()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 10,
            directContributorCount: 10,
            itemContributorCount: 0,
            categories: Array.Empty<RankingCategoryCoverage>());

        RankingEvidence evidence = RankingEligibilityPolicy.Initial.EvaluatePark(input);

        Assert.Equal(RankingEvidenceLevel.Eligible, evidence.Level);
        Assert.True(evidence.IsEligibleForMainRanking);
        Assert.Null(evidence.IneligibilityReason);
        Assert.Equal(0, evidence.EligibleItemCount);
        Assert.Equal(0, evidence.EligibleCategoryCount);
    }

    [Fact]
    public void EvaluatePark_WhenItemComponentIsIneligible_ShouldBaseEvidenceOnDirectRatingsOnly()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 100,
            directContributorCount: 10,
            itemContributorCount: 90,
            categories: new[] { new RankingCategoryCoverage(5, 0) });

        RankingEvidence evidence = RankingEligibilityPolicy.Initial.EvaluatePark(input);

        Assert.Equal(RankingEvidenceLevel.Eligible, evidence.Level);
        Assert.True(evidence.IsEligibleForMainRanking);
        Assert.Equal(10, evidence.UniqueContributorCount);
        Assert.Equal(10, evidence.RatingObservationCount);
        Assert.Equal(90, evidence.ItemContributorCount);
        Assert.Equal(0, evidence.EligibleItemCount);
    }

    [Fact]
    public void EvaluatePark_WhenItemComponentIsEligible_ShouldUseTheContributorUnion()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 100,
            directContributorCount: 10,
            itemContributorCount: 90,
            categories: new[]
            {
                new RankingCategoryCoverage(3, 3),
                new RankingCategoryCoverage(2, 2),
            });

        RankingEvidence evidence = RankingEligibilityPolicy.Initial.EvaluatePark(input);

        Assert.Equal(RankingEvidenceLevel.StrongEvidence, evidence.Level);
        Assert.True(evidence.IsEligibleForMainRanking);
        Assert.Equal(100, evidence.UniqueContributorCount);
        Assert.Equal(100, evidence.RatingObservationCount);
    }

    [Fact]
    public void EvaluatePark_WhenUniqueUnionExceedsBothContributorSets_ShouldRejectInput()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 100,
            directContributorCount: 10,
            itemContributorCount: 0,
            categories: Array.Empty<RankingCategoryCoverage>());

        Assert.Throws<ArgumentException>(
            () => RankingEligibilityPolicy.Initial.EvaluatePark(input));
    }

    [Fact]
    public void EvaluatePark_WhenObservationsDoNotCoverBothContributorSets_ShouldRejectInput()
    {
        ParkRankingEvidenceInput input = new ParkRankingEvidenceInput(
            UniqueContributorCount: 10,
            RatingObservationCount: 10,
            DirectParkContributorCount: 10,
            ItemContributorCount: 10,
            ItemCategories: new[]
            {
                new RankingCategoryCoverage(3, 3),
                new RankingCategoryCoverage(2, 2),
            },
            IsSingleCategoryParkException: false,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            AggregateIntegrityIsValid: true);

        Assert.Throws<ArgumentException>(
            () => RankingEligibilityPolicy.Initial.EvaluatePark(input));
    }

    [Fact]
    public void EvaluatePark_WhenObservationsCoverContributorsButNotEligibleItems_ShouldRejectInput()
    {
        ParkRankingEvidenceInput input = new ParkRankingEvidenceInput(
            UniqueContributorCount: 10,
            RatingObservationCount: 20,
            DirectParkContributorCount: 10,
            ItemContributorCount: 10,
            ItemCategories: new[]
            {
                new RankingCategoryCoverage(3, 3),
                new RankingCategoryCoverage(2, 2),
            },
            IsSingleCategoryParkException: false,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            AggregateIntegrityIsValid: true);

        Assert.Throws<ArgumentException>(
            () => RankingEligibilityPolicy.Initial.EvaluatePark(input));
    }

    [Fact]
    public void EvaluatePark_WhenObservationsExceedOneCurrentRatingPerPublicTarget_ShouldRejectInput()
    {
        ParkRankingEvidenceInput input = new ParkRankingEvidenceInput(
            UniqueContributorCount: 10,
            RatingObservationCount: 1_000,
            DirectParkContributorCount: 10,
            ItemContributorCount: 10,
            ItemCategories: new[]
            {
                new RankingCategoryCoverage(3, 3),
                new RankingCategoryCoverage(2, 2),
            },
            IsSingleCategoryParkException: false,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            AggregateIntegrityIsValid: true);

        Assert.Throws<ArgumentException>(
            () => RankingEligibilityPolicy.Initial.EvaluatePark(input));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void EvaluateRankingPublication_ShouldRequireThreeComparableEntries(
        int eligibleEntryCount,
        bool expectedEligibility)
    {
        RankingPublicationEligibility eligibility =
            RankingEligibilityPolicy.Initial.EvaluateRankingPublication(eligibleEntryCount);

        Assert.Equal(expectedEligibility, eligibility.IsEligible);
        Assert.Equal(
            expectedEligibility ? null : RankingIneligibilityReason.TooFewComparableEntries,
            eligibility.IneligibilityReason);
    }

    [Fact]
    public void AreScoresTied_ShouldUseStrictEpsilonWithoutRoundingScores()
    {
        RankingEligibilityPolicy policy = RankingEligibilityPolicy.Initial;

        Assert.True(policy.AreScoresTied(4d, 4.000099d));
        Assert.False(policy.AreScoresTied(4d, 4.0001d));
    }

    [Theory]
    [InlineData(double.NaN, 4d)]
    [InlineData(double.PositiveInfinity, 4d)]
    [InlineData(4d, double.NegativeInfinity)]
    public void AreScoresTied_WhenScoreIsNotFinite_ShouldRejectInput(double leftScore, double rightScore)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankingEligibilityPolicy.Initial.AreScoresTied(leftScore, rightScore));
    }

    [Theory]
    [InlineData(0.49d, 0.5d)]
    [InlineData(5d, 5.01d)]
    public void AreScoresTied_WhenScoreIsOutsideRatingScale_ShouldRejectInput(double leftScore, double rightScore)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankingEligibilityPolicy.Initial.AreScoresTied(leftScore, rightScore));
    }

    [Fact]
    public void EvaluateSimpleTarget_WhenPolicyUsesCustomThresholds_ShouldApplyThem()
    {
        RankingEligibilityPolicy policy = new RankingEligibilityPolicy(
            RatingMethodologyVersion.Parse("ratings-test-thresholds"),
            2,
            5,
            8,
            12,
            2,
            3,
            1,
            1,
            0.001m);

        RankingEvidence evidence = policy.EvaluateSimpleTarget(CreateSimpleInput(5, 5));

        Assert.Equal(RankingEvidenceLevel.Eligible, evidence.Level);
        Assert.True(evidence.IsEligibleForMainRanking);
    }

    [Fact]
    public void Constructor_WhenContributorThresholdsAreNotIncreasing_ShouldRejectPolicy()
    {
        Assert.Throws<ArgumentException>(
            () => new RankingEligibilityPolicy(
                RatingMethodologyVersion.Parse("ratings-invalid"),
                3,
                3,
                30,
                100,
                3,
                5,
                2,
                2,
                0.0001m));
    }

    [Fact]
    public void EvaluateParkItemComponent_WhenCategoryHasNoPublicItem_ShouldRejectInput()
    {
        ParkRankingEvidenceInput input = CreateParkInput(
            uniqueContributorCount: 10,
            directContributorCount: 10,
            itemContributorCount: 0,
            categories: new[] { new RankingCategoryCoverage(0, 0) });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankingEligibilityPolicy.Initial.EvaluateParkItemComponent(input));
    }

    [Fact]
    public void Constructor_WhenPolicyUsesCustomVersion_ShouldPropagateItToEvidence()
    {
        RatingMethodologyVersion version = RatingMethodologyVersion.Parse("ratings-test-02");
        RankingEligibilityPolicy policy = new RankingEligibilityPolicy(
            version,
            3,
            10,
            30,
            100,
            3,
            5,
            2,
            2,
            0.0001m);

        RankingEvidence evidence = policy.EvaluateSimpleTarget(CreateSimpleInput(10, 10));

        Assert.Equal(version, evidence.MethodologyVersion);
    }

    private static SimpleRankingEvidenceInput CreateSimpleInput(int contributorCount, int observationCount)
    {
        return new SimpleRankingEvidenceInput(
            contributorCount,
            observationCount,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            AggregateIntegrityIsValid: true);
    }

    private static ParkRankingEvidenceInput CreateParkInput(
        int uniqueContributorCount,
        int directContributorCount,
        int itemContributorCount,
        IReadOnlyCollection<RankingCategoryCoverage> categories,
        bool isSingleCategoryParkException = false)
    {
        int eligibleItemCount = categories.Sum(static category => category.EligibleItemCount);
        int itemObservationMinimum = Math.Max(
            itemContributorCount,
            checked(eligibleItemCount * RankingEligibilityPolicy.Initial.EligibleMinUniqueContributors));

        return new ParkRankingEvidenceInput(
            uniqueContributorCount,
            RatingObservationCount: Math.Max(
                uniqueContributorCount,
                directContributorCount + itemObservationMinimum),
            directContributorCount,
            itemContributorCount,
            categories,
            isSingleCategoryParkException,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            AggregateIntegrityIsValid: true);
    }
}
