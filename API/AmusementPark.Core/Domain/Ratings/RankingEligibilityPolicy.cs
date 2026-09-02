using System.Diagnostics.CodeAnalysis;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Politique pure et versionnée qui décide si une preuve autorise un rang public.
/// </summary>
public sealed class RankingEligibilityPolicy
{
    public static readonly RatingMethodologyVersion InitialMethodologyVersion =
        RatingMethodologyVersion.Parse("ratings-2026-01");

    public static readonly RankingEligibilityPolicy Initial = new RankingEligibilityPolicy(
        InitialMethodologyVersion,
        provisionalMinUniqueContributors: 3,
        eligibleMinUniqueContributors: 10,
        establishedMinUniqueContributors: 30,
        strongEvidenceMinUniqueContributors: 100,
        minimumEligibleEntriesPerRanking: 3,
        minimumEligibleItemsForParkItemComponent: 5,
        minimumEligibleItemsPerCategory: 2,
        minimumEligibleCategories: 2,
        scoreTieEpsilon: 0.0001m);

    public RankingEligibilityPolicy(
        RatingMethodologyVersion version,
        int provisionalMinUniqueContributors,
        int eligibleMinUniqueContributors,
        int establishedMinUniqueContributors,
        int strongEvidenceMinUniqueContributors,
        int minimumEligibleEntriesPerRanking,
        int minimumEligibleItemsForParkItemComponent,
        int minimumEligibleItemsPerCategory,
        int minimumEligibleCategories,
        decimal scoreTieEpsilon)
    {
        ValidateThresholds(
            provisionalMinUniqueContributors,
            eligibleMinUniqueContributors,
            establishedMinUniqueContributors,
            strongEvidenceMinUniqueContributors,
            minimumEligibleEntriesPerRanking,
            minimumEligibleItemsForParkItemComponent,
            minimumEligibleItemsPerCategory,
            minimumEligibleCategories,
            scoreTieEpsilon);

        _ = version.Value;
        this.Version = version;
        this.ProvisionalMinUniqueContributors = provisionalMinUniqueContributors;
        this.EligibleMinUniqueContributors = eligibleMinUniqueContributors;
        this.EstablishedMinUniqueContributors = establishedMinUniqueContributors;
        this.StrongEvidenceMinUniqueContributors = strongEvidenceMinUniqueContributors;
        this.MinimumEligibleEntriesPerRanking = minimumEligibleEntriesPerRanking;
        this.MinimumEligibleItemsForParkItemComponent = minimumEligibleItemsForParkItemComponent;
        this.MinimumEligibleItemsPerCategory = minimumEligibleItemsPerCategory;
        this.MinimumEligibleCategories = minimumEligibleCategories;
        this.ScoreTieEpsilon = scoreTieEpsilon;
    }

    public RatingMethodologyVersion Version { get; }

    public int ProvisionalMinUniqueContributors { get; }

    public int EligibleMinUniqueContributors { get; }

    public int EstablishedMinUniqueContributors { get; }

    public int StrongEvidenceMinUniqueContributors { get; }

    public int MinimumEligibleEntriesPerRanking { get; }

    public int MinimumEligibleItemsForParkItemComponent { get; }

    public int MinimumEligibleItemsPerCategory { get; }

    public int MinimumEligibleCategories { get; }

    public decimal ScoreTieEpsilon { get; }

    public static bool TryResolve(
        RatingMethodologyVersion version,
        [NotNullWhen(true)] out RankingEligibilityPolicy? policy)
    {
        if (version == Initial.Version)
        {
            policy = Initial;
            return true;
        }

        policy = null;
        return false;
    }

    public bool IsEligibleSnapshotEvidence(
        RatingTargetType targetType,
        RankingEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (!Enum.IsDefined(targetType) ||
            evidence.MethodologyVersion != this.Version ||
            !evidence.IsEligibleForMainRanking ||
            evidence.IneligibilityReason.HasValue)
        {
            return false;
        }

        try
        {
            ValidateObservationCounts(
                evidence.UniqueContributorCount,
                evidence.RatingObservationCount);
        }
        catch (ArgumentException)
        {
            return false;
        }

        RankingEvidenceLevel expectedLevel = this.ResolveEvidenceLevel(
            evidence.UniqueContributorCount);
        if (evidence.Level != expectedLevel ||
            expectedLevel is not (RankingEvidenceLevel.Eligible
                or RankingEvidenceLevel.Established
                or RankingEvidenceLevel.StrongEvidence) ||
            evidence.NextContributorThreshold != this.ResolveNextContributorThreshold(expectedLevel))
        {
            return false;
        }

        return targetType switch
        {
            RatingTargetType.Park => IsEligibleParkSnapshotEvidence(
                evidence,
                this.EligibleMinUniqueContributors,
                this.MinimumEligibleItemsForParkItemComponent,
                this.MinimumEligibleCategories),
            RatingTargetType.ParkItem => IsEligibleSimpleSnapshotEvidence(evidence),
            _ => false,
        };
    }

    public RankingEvidence EvaluateSimpleTarget(SimpleRankingEvidenceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateObservationCounts(input.UniqueContributorCount, input.RatingObservationCount);
        bool aggregateIntegrityIsValid = input.AggregateIntegrityIsValid
            && input.RatingObservationCount == input.UniqueContributorCount;

        RankingIneligibilityReason? exclusionReason = ResolveExclusionReason(
            input.TargetCanReceiveVisitorRatings,
            input.IsExcludedByModeration,
            aggregateIntegrityIsValid);
        if (exclusionReason.HasValue)
        {
            return this.CreateSimpleEvidence(input, RankingEvidenceLevel.Excluded, false, exclusionReason);
        }

        RankingEvidenceLevel level = this.ResolveEvidenceLevel(input.UniqueContributorCount);
        bool isEligible = level is RankingEvidenceLevel.Eligible
            or RankingEvidenceLevel.Established
            or RankingEvidenceLevel.StrongEvidence;
        RankingIneligibilityReason? ineligibilityReason = ResolveVolumeIneligibilityReason(level);

        return this.CreateSimpleEvidence(input, level, isEligible, ineligibilityReason);
    }

    public bool TryEvaluateSimpleTarget(SimpleRankingEvidenceInput? input, out RankingEvidence? evidence)
    {
        if (input is null)
        {
            evidence = null;
            return false;
        }

        try
        {
            evidence = this.EvaluateSimpleTarget(input);
            return true;
        }
        catch (ArgumentException)
        {
            evidence = null;
            return false;
        }
        catch (OverflowException)
        {
            evidence = null;
            return false;
        }
    }

    public RankingEvidence EvaluatePark(ParkRankingEvidenceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        this.ValidateParkInput(input);

        ParkItemComponentEligibility itemComponent = this.EvaluateParkItemComponentValidated(input);
        return this.EvaluateParkValidated(input, itemComponent);
    }

    public ParkRankingEvaluation EvaluateParkRanking(
        ParkRankingEvidenceInput input,
        double? directParkScore,
        double? parkItemsScore)
    {
        ArgumentNullException.ThrowIfNull(input);
        this.ValidateParkInput(input);
        ValidateOptionalScore(directParkScore, nameof(directParkScore));
        ValidateOptionalScore(parkItemsScore, nameof(parkItemsScore));

        ParkItemComponentEligibility itemComponent = this.EvaluateParkItemComponentValidated(input);
        RankingEvidence evidence = this.EvaluateParkValidated(input, itemComponent);
        bool directComponentContributes =
            input.DirectParkContributorCount >= this.EligibleMinUniqueContributors
            && directParkScore.HasValue;
        bool itemComponentContributes = itemComponent.IsEligible && parkItemsScore.HasValue;
        ParkRankingCompositionMode compositionMode = ResolveCompositionMode(
            directComponentContributes,
            itemComponentContributes);
        double score = RatingScoreCalculator.CalculateCompositeParkScore(
            directComponentContributes ? directParkScore : null,
            itemComponentContributes ? parkItemsScore : null);

        return new ParkRankingEvaluation(evidence, itemComponent, compositionMode, score);
    }

    public bool TryEvaluateParkRanking(
        ParkRankingEvidenceInput? input,
        double? directParkScore,
        double? parkItemsScore,
        [NotNullWhen(true)] out ParkRankingEvaluation? evaluation)
    {
        if (input is null)
        {
            evaluation = null;
            return false;
        }

        try
        {
            evaluation = this.EvaluateParkRanking(input, directParkScore, parkItemsScore);
            return true;
        }
        catch (ArgumentException)
        {
            evaluation = null;
            return false;
        }
        catch (OverflowException)
        {
            evaluation = null;
            return false;
        }
    }

    public int ResolveMainRankingEligibilityContributorCount(
        RatingTargetType targetType,
        RankingEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        return targetType == RatingTargetType.Park
            && evidence.IneligibilityReason == RankingIneligibilityReason.TooFewUniqueContributors
            && evidence.DirectParkContributorCount.HasValue
                ? evidence.DirectParkContributorCount.Value
                : evidence.UniqueContributorCount;
    }

    private RankingEvidence EvaluateParkValidated(
        ParkRankingEvidenceInput input,
        ParkItemComponentEligibility itemComponent)
    {
        RankingIneligibilityReason? exclusionReason = ResolveExclusionReason(
            input.TargetCanReceiveVisitorRatings,
            input.IsExcludedByModeration,
            input.AggregateIntegrityIsValid);
        if (exclusionReason.HasValue)
        {
            return this.CreateParkEvidence(
                input,
                itemComponent,
                input.UniqueContributorCount,
                input.RatingObservationCount,
                RankingEvidenceLevel.Excluded,
                false,
                exclusionReason);
        }

        bool directComponentIsEligible = input.DirectParkContributorCount >= this.EligibleMinUniqueContributors;
        int activeContributorCount = ResolveActiveContributorCount(
            input,
            directComponentIsEligible,
            itemComponent.IsEligible);
        int activeObservationCount = ResolveActiveObservationCount(
            input,
            directComponentIsEligible,
            itemComponent.IsEligible);
        RankingEvidenceLevel level = this.ResolveEvidenceLevel(activeContributorCount);
        if (level is RankingEvidenceLevel.NoEvidence or RankingEvidenceLevel.Insufficient or RankingEvidenceLevel.Provisional)
        {
            return this.CreateParkEvidence(
                input,
                itemComponent,
                activeContributorCount,
                activeObservationCount,
                level,
                false,
                ResolveVolumeIneligibilityReason(level));
        }

        if (!directComponentIsEligible)
        {
            return this.CreateParkEvidence(
                input,
                itemComponent,
                activeContributorCount,
                activeObservationCount,
                RankingEvidenceLevel.Provisional,
                false,
                RankingIneligibilityReason.TooFewUniqueContributors);
        }

        return this.CreateParkEvidence(
            input,
            itemComponent,
            activeContributorCount,
            activeObservationCount,
            level,
            true,
            null);
    }

    public bool TryEvaluatePark(ParkRankingEvidenceInput? input, out RankingEvidence? evidence)
    {
        if (input is null)
        {
            evidence = null;
            return false;
        }

        try
        {
            evidence = this.EvaluatePark(input);
            return true;
        }
        catch (ArgumentException)
        {
            evidence = null;
            return false;
        }
        catch (OverflowException)
        {
            evidence = null;
            return false;
        }
    }

    public ParkItemComponentEligibility EvaluateParkItemComponent(ParkRankingEvidenceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        this.ValidateParkInput(input);

        return this.EvaluateParkItemComponentValidated(input);
    }

    private ParkItemComponentEligibility EvaluateParkItemComponentValidated(ParkRankingEvidenceInput input)
    {
        int eligibleItemCount = 0;
        int eligibleCategoryCount = 0;
        foreach (RankingCategoryCoverage category in input.ItemCategories)
        {
            eligibleItemCount = checked(eligibleItemCount + category.EligibleItemCount);
            if (IsCategoryCovered(category, this.MinimumEligibleItemsPerCategory))
            {
                eligibleCategoryCount++;
            }
        }

        if (input.ItemContributorCount < this.EligibleMinUniqueContributors)
        {
            return new ParkItemComponentEligibility(
                false,
                eligibleItemCount,
                eligibleCategoryCount,
                RankingIneligibilityReason.TooFewUniqueContributors);
        }

        if (eligibleItemCount < this.MinimumEligibleItemsForParkItemComponent)
        {
            return new ParkItemComponentEligibility(
                false,
                eligibleItemCount,
                eligibleCategoryCount,
                RankingIneligibilityReason.InsufficientItemCoverage);
        }

        int requiredCategoryCount = input.IsSingleCategoryParkException
            ? 1
            : this.MinimumEligibleCategories;
        if (eligibleCategoryCount < requiredCategoryCount)
        {
            return new ParkItemComponentEligibility(
                false,
                eligibleItemCount,
                eligibleCategoryCount,
                RankingIneligibilityReason.InsufficientCategoryCoverage);
        }

        return new ParkItemComponentEligibility(true, eligibleItemCount, eligibleCategoryCount, null);
    }

    private static ParkRankingCompositionMode ResolveCompositionMode(
        bool directComponentContributes,
        bool itemComponentContributes)
    {
        if (directComponentContributes && itemComponentContributes)
        {
            return ParkRankingCompositionMode.DirectAndItems;
        }

        if (directComponentContributes)
        {
            return ParkRankingCompositionMode.DirectOnly;
        }

        return itemComponentContributes
            ? ParkRankingCompositionMode.ItemsOnly
            : ParkRankingCompositionMode.None;
    }

    public RankingPublicationEligibility EvaluateRankingPublication(int eligibleEntryCount)
    {
        if (eligibleEntryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eligibleEntryCount));
        }

        return eligibleEntryCount >= this.MinimumEligibleEntriesPerRanking
            ? new RankingPublicationEligibility(true, null)
            : new RankingPublicationEligibility(false, RankingIneligibilityReason.TooFewComparableEntries);
    }

    public bool AreScoresTied(double leftScore, double rightScore)
    {
        ValidateScore(leftScore, nameof(leftScore));
        ValidateScore(rightScore, nameof(rightScore));

        decimal difference = Math.Abs((decimal)leftScore - (decimal)rightScore);
        return difference < this.ScoreTieEpsilon;
    }

    private RankingEvidenceLevel ResolveEvidenceLevel(int uniqueContributorCount)
    {
        if (uniqueContributorCount == 0)
        {
            return RankingEvidenceLevel.NoEvidence;
        }

        if (uniqueContributorCount < this.ProvisionalMinUniqueContributors)
        {
            return RankingEvidenceLevel.Insufficient;
        }

        if (uniqueContributorCount < this.EligibleMinUniqueContributors)
        {
            return RankingEvidenceLevel.Provisional;
        }

        if (uniqueContributorCount < this.EstablishedMinUniqueContributors)
        {
            return RankingEvidenceLevel.Eligible;
        }

        if (uniqueContributorCount < this.StrongEvidenceMinUniqueContributors)
        {
            return RankingEvidenceLevel.Established;
        }

        return RankingEvidenceLevel.StrongEvidence;
    }

    private void ValidateParkInput(ParkRankingEvidenceInput input)
    {
        ValidateObservationCounts(input.UniqueContributorCount, input.RatingObservationCount);
        ValidateCount(input.DirectParkContributorCount, nameof(input.DirectParkContributorCount));
        ValidateCount(input.ItemContributorCount, nameof(input.ItemContributorCount));
        ArgumentNullException.ThrowIfNull(input.ItemCategories);

        if (input.DirectParkContributorCount > input.UniqueContributorCount)
        {
            throw new ArgumentOutOfRangeException(nameof(input.DirectParkContributorCount));
        }

        if (input.ItemContributorCount > input.UniqueContributorCount)
        {
            throw new ArgumentOutOfRangeException(nameof(input.ItemContributorCount));
        }

        long maximumContributorUnion = (long)input.DirectParkContributorCount + input.ItemContributorCount;
        if (input.UniqueContributorCount > maximumContributorUnion)
        {
            throw new ArgumentException(
                "Unique park contributors cannot exceed the union capacity of direct and item contributors.",
                nameof(input));
        }

        int publicItemCount = 0;
        int eligibleItemCount = 0;
        foreach (RankingCategoryCoverage category in input.ItemCategories)
        {
            if (category is null)
            {
                throw new ArgumentException("Item category coverage cannot contain null values.", nameof(input));
            }

            if (category.PublicItemCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(category.PublicItemCount));
            }

            ValidateCount(category.EligibleItemCount, nameof(category.EligibleItemCount));
            if (category.EligibleItemCount > category.PublicItemCount)
            {
                throw new ArgumentException(
                    "Eligible item count cannot exceed public item count.",
                    nameof(input));
            }

            publicItemCount = checked(publicItemCount + category.PublicItemCount);
            eligibleItemCount = checked(eligibleItemCount + category.EligibleItemCount);
        }

        if (eligibleItemCount > 0 && input.ItemContributorCount < this.EligibleMinUniqueContributors)
        {
            throw new ArgumentException(
                "Eligible items require at least the eligible contributor threshold across item ratings.",
                nameof(input));
        }

        long minimumItemObservationCount = Math.Max(
            input.ItemContributorCount,
            checked((long)eligibleItemCount * this.EligibleMinUniqueContributors));
        long minimumObservationCount = input.DirectParkContributorCount + minimumItemObservationCount;
        if (input.RatingObservationCount < minimumObservationCount)
        {
            throw new ArgumentException(
                "Park observations cannot be lower than the minimum required by both active component inputs.",
                nameof(input));
        }

        long maximumItemObservationCount = checked((long)input.ItemContributorCount * publicItemCount);
        long maximumObservationCount = input.DirectParkContributorCount + maximumItemObservationCount;
        if (input.RatingObservationCount > maximumObservationCount)
        {
            throw new ArgumentException(
                "Park observations cannot exceed one current rating per contributor and public target.",
                nameof(input));
        }

        if (input.IsSingleCategoryParkException && input.ItemCategories.Count != 1)
        {
            throw new ArgumentException(
                "The single-category exception requires exactly one public item category.",
                nameof(input));
        }

    }

    private static bool IsCategoryCovered(RankingCategoryCoverage category, int minimumEligibleItemsPerCategory)
    {
        return category.EligibleItemCount >= minimumEligibleItemsPerCategory
            || (category.PublicItemCount == 1 && category.EligibleItemCount == 1);
    }

    private static int ResolveActiveContributorCount(
        ParkRankingEvidenceInput input,
        bool directComponentIsEligible,
        bool itemComponentIsEligible)
    {
        if (directComponentIsEligible && itemComponentIsEligible)
        {
            return input.UniqueContributorCount;
        }

        return itemComponentIsEligible
            ? input.ItemContributorCount
            : input.DirectParkContributorCount;
    }

    private static int ResolveActiveObservationCount(
        ParkRankingEvidenceInput input,
        bool directComponentIsEligible,
        bool itemComponentIsEligible)
    {
        if (directComponentIsEligible && itemComponentIsEligible)
        {
            return input.RatingObservationCount;
        }

        return itemComponentIsEligible
            ? input.RatingObservationCount - input.DirectParkContributorCount
            : input.DirectParkContributorCount;
    }

    private RankingEvidence CreateSimpleEvidence(
        SimpleRankingEvidenceInput input,
        RankingEvidenceLevel level,
        bool isEligible,
        RankingIneligibilityReason? ineligibilityReason)
    {
        RankingEvidence evidence = new RankingEvidence(
            level,
            isEligible,
            input.UniqueContributorCount,
            input.RatingObservationCount,
            null,
            null,
            null,
            null,
            this.Version,
            ineligibilityReason);

        return evidence with { NextContributorThreshold = this.ResolveNextContributorThreshold(level) };
    }

    private RankingEvidence CreateParkEvidence(
        ParkRankingEvidenceInput input,
        ParkItemComponentEligibility itemComponent,
        int activeContributorCount,
        int activeObservationCount,
        RankingEvidenceLevel level,
        bool isEligible,
        RankingIneligibilityReason? ineligibilityReason)
    {
        RankingEvidence evidence = new RankingEvidence(
            level,
            isEligible,
            activeContributorCount,
            activeObservationCount,
            input.DirectParkContributorCount,
            input.ItemContributorCount,
            itemComponent.EligibleItemCount,
            itemComponent.EligibleCategoryCount,
            this.Version,
            ineligibilityReason);

        return evidence with
        {
            NextContributorThreshold = this.ResolveNextContributorThreshold(level),
            IsSingleCategoryParkException = input.IsSingleCategoryParkException,
            PublicItemCategoryCount = input.ItemCategories.Count,
        };
    }

    private int? ResolveNextContributorThreshold(RankingEvidenceLevel level)
    {
        return level switch
        {
            RankingEvidenceLevel.NoEvidence or RankingEvidenceLevel.Insufficient =>
                this.ProvisionalMinUniqueContributors,
            RankingEvidenceLevel.Provisional => this.EligibleMinUniqueContributors,
            RankingEvidenceLevel.Eligible => this.EstablishedMinUniqueContributors,
            RankingEvidenceLevel.Established => this.StrongEvidenceMinUniqueContributors,
            _ => null,
        };
    }

    private static RankingIneligibilityReason? ResolveExclusionReason(
        bool targetCanReceiveVisitorRatings,
        bool isExcludedByModeration,
        bool aggregateIntegrityIsValid)
    {
        if (!targetCanReceiveVisitorRatings)
        {
            return RankingIneligibilityReason.TargetUnavailable;
        }

        if (isExcludedByModeration)
        {
            return RankingIneligibilityReason.TargetExcluded;
        }

        if (!aggregateIntegrityIsValid)
        {
            return RankingIneligibilityReason.AggregateIntegrityFailure;
        }

        return null;
    }

    private static RankingIneligibilityReason? ResolveVolumeIneligibilityReason(RankingEvidenceLevel level)
    {
        return level switch
        {
            RankingEvidenceLevel.NoEvidence => RankingIneligibilityReason.NoRatings,
            RankingEvidenceLevel.Insufficient or RankingEvidenceLevel.Provisional =>
                RankingIneligibilityReason.TooFewUniqueContributors,
            _ => null,
        };
    }

    private static bool IsEligibleSimpleSnapshotEvidence(RankingEvidence evidence)
    {
        return evidence.RatingObservationCount == evidence.UniqueContributorCount &&
            !evidence.DirectParkContributorCount.HasValue &&
            !evidence.ItemContributorCount.HasValue &&
            !evidence.EligibleItemCount.HasValue &&
            !evidence.EligibleCategoryCount.HasValue &&
            !evidence.IsSingleCategoryParkException.HasValue &&
            !evidence.PublicItemCategoryCount.HasValue;
    }

    private static bool IsEligibleParkSnapshotEvidence(
        RankingEvidence evidence,
        int eligibleMinUniqueContributors,
        int minimumEligibleItemsForParkItemComponent,
        int minimumEligibleCategories)
    {
        if (evidence.DirectParkContributorCount is not int directParkContributorCount ||
            directParkContributorCount < eligibleMinUniqueContributors ||
            directParkContributorCount > evidence.UniqueContributorCount ||
            evidence.ItemContributorCount is not int itemContributorCount ||
            itemContributorCount < 0 ||
            evidence.EligibleItemCount is not int eligibleItemCount ||
            eligibleItemCount < 0 ||
            evidence.EligibleCategoryCount is not int eligibleCategoryCount ||
            eligibleCategoryCount < 0 ||
            eligibleCategoryCount > eligibleItemCount ||
            evidence.PublicItemCategoryCount is not int publicItemCategoryCount ||
            publicItemCategoryCount < 0 ||
            eligibleCategoryCount > publicItemCategoryCount ||
            evidence.IsSingleCategoryParkException is not bool isSingleCategoryParkException ||
            (isSingleCategoryParkException && publicItemCategoryCount != 1) ||
            (eligibleItemCount > 0 &&
                (publicItemCategoryCount == 0 ||
                    itemContributorCount < eligibleMinUniqueContributors)))
        {
            return false;
        }

        int minimumEligibleCategoryCount = isSingleCategoryParkException
            ? 1
            : minimumEligibleCategories;
        bool itemComponentContributed =
            evidence.UniqueContributorCount > directParkContributorCount ||
            evidence.RatingObservationCount > directParkContributorCount;
        bool itemComponentMustContribute =
            itemContributorCount >= eligibleMinUniqueContributors &&
            eligibleItemCount >= minimumEligibleItemsForParkItemComponent &&
            eligibleCategoryCount >= minimumEligibleCategoryCount;
        if (!itemComponentContributed)
        {
            return !itemComponentMustContribute &&
                evidence.UniqueContributorCount == directParkContributorCount &&
                evidence.RatingObservationCount == directParkContributorCount;
        }

        long minimumItemObservationCount = Math.Max(
            itemContributorCount,
            (long)eligibleItemCount * eligibleMinUniqueContributors);
        return itemContributorCount >= eligibleMinUniqueContributors &&
            eligibleItemCount >= minimumEligibleItemsForParkItemComponent &&
            eligibleCategoryCount >= minimumEligibleCategoryCount &&
            itemContributorCount <= evidence.UniqueContributorCount &&
            evidence.UniqueContributorCount <= (long)directParkContributorCount + itemContributorCount &&
            evidence.RatingObservationCount >= (long)directParkContributorCount + minimumItemObservationCount;
    }

    private static void ValidateObservationCounts(int uniqueContributorCount, int ratingObservationCount)
    {
        ValidateCount(uniqueContributorCount, nameof(uniqueContributorCount));
        ValidateCount(ratingObservationCount, nameof(ratingObservationCount));
        if (ratingObservationCount < uniqueContributorCount)
        {
            throw new ArgumentException(
                "Rating observation count cannot be lower than unique contributor count.",
                nameof(ratingObservationCount));
        }

        if (uniqueContributorCount == 0 && ratingObservationCount > 0)
        {
            throw new ArgumentException(
                "Rating observations require at least one unique contributor.",
                nameof(ratingObservationCount));
        }
    }

    private static void ValidateOptionalScore(double? value, string parameterName)
    {
        if (value.HasValue)
        {
            ValidateScore(value.Value, parameterName);
        }
    }

    private static void ValidateCount(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateScore(double value, string parameterName)
    {
        if (!double.IsFinite(value)
            || value < RatingValue.MinimumHalfSteps / 2d
            || value > RatingValue.MaximumHalfSteps / 2d)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateThresholds(
        int provisionalMinUniqueContributors,
        int eligibleMinUniqueContributors,
        int establishedMinUniqueContributors,
        int strongEvidenceMinUniqueContributors,
        int minimumEligibleEntriesPerRanking,
        int minimumEligibleItemsForParkItemComponent,
        int minimumEligibleItemsPerCategory,
        int minimumEligibleCategories,
        decimal scoreTieEpsilon)
    {
        if (provisionalMinUniqueContributors <= 0
            || eligibleMinUniqueContributors <= provisionalMinUniqueContributors
            || establishedMinUniqueContributors <= eligibleMinUniqueContributors
            || strongEvidenceMinUniqueContributors <= establishedMinUniqueContributors)
        {
            throw new ArgumentException("Contributor thresholds must be positive and strictly increasing.");
        }

        if (minimumEligibleEntriesPerRanking <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumEligibleEntriesPerRanking));
        }

        if (minimumEligibleItemsForParkItemComponent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumEligibleItemsForParkItemComponent));
        }

        if (minimumEligibleItemsPerCategory <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumEligibleItemsPerCategory));
        }

        if (minimumEligibleCategories <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumEligibleCategories));
        }

        if (scoreTieEpsilon <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreTieEpsilon));
        }
    }
}
