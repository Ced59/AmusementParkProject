namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Normalise un portefeuille d'attractions sans masquer le membre le moins servi.
/// </summary>
public sealed class ParkFitGroupCompatibilitySubscoreEvaluator
{
    public const decimal EveryoneTogetherValue = 100m;
    public const decimal PossibleWithSplitValue = 75m;
    public const decimal PartialValue = 35m;
    public const decimal NoneValue = 0m;

    public ParkFitSubscore Evaluate(
        IReadOnlyCollection<GroupAttractionCompatibility> attractionCompatibilities,
        DateOnly evaluationDate)
    {
        ArgumentNullException.ThrowIfNull(attractionCompatibilities);

        if (attractionCompatibilities.Count == 0)
        {
            return BuildUnknown(evaluationDate);
        }

        List<GroupAttractionCompatibility> snapshot = attractionCompatibilities.ToList();
        if (snapshot.Any(static compatibility => compatibility is null))
        {
            throw new ArgumentException(
                "Attraction compatibilities cannot contain null entries.",
                nameof(attractionCompatibilities));
        }

        ValidateCompatibilitySet(snapshot, attractionCompatibilities, evaluationDate);

        List<GroupAttractionCompatibility> knownGroupOutcomes = snapshot
            .Where(static compatibility =>
                compatibility.State != GroupAttractionCompatibilityState.Unknown)
            .ToList();
        if (knownGroupOutcomes.Count == 0)
        {
            return BuildUnknown(evaluationDate);
        }

        decimal groupOutcomeAverage = knownGroupOutcomes.Average(
            static compatibility => GetGroupOutcomeValue(compatibility.State));
        decimal? minimumMemberValue = CalculateMinimumMemberValue(snapshot);
        if (!minimumMemberValue.HasValue)
        {
            return new ParkFitSubscore(
                ParkFitSubscoreKind.GroupCompatibility,
                ParkFitSubscoreState.Unknown,
                null,
                CalculateCoveragePercent(snapshot, knownGroupOutcomes.Count),
                ParkFitDataConfidence.Unknown,
                new[] { ParkFitSubscoreReasonCode.NoKnownFact },
                evaluationDate);
        }

        decimal coveragePercent = CalculateCoveragePercent(
            snapshot,
            knownGroupOutcomes.Count);
        decimal value = Math.Min(groupOutcomeAverage, minimumMemberValue.Value);
        List<ParkFitSubscoreReasonCode> reasons = new List<ParkFitSubscoreReasonCode>
        {
            ParkFitSubscoreReasonCode.KnownFactsNormalized,
        };
        if (knownGroupOutcomes.Count != snapshot.Count || coveragePercent < 100m)
        {
            reasons.Add(ParkFitSubscoreReasonCode.UnknownFactsExcluded);
        }

        if (minimumMemberValue.Value < groupOutcomeAverage)
        {
            reasons.Add(ParkFitSubscoreReasonCode.MinimumMemberBoundApplied);
        }

        return new ParkFitSubscore(
            ParkFitSubscoreKind.GroupCompatibility,
            ParkFitSubscoreState.Known,
            Round(value),
            Round(coveragePercent),
            knownGroupOutcomes.Min(static compatibility => compatibility.Confidence),
            reasons,
            evaluationDate);
    }

    private static void ValidateCompatibilitySet(
        IReadOnlyCollection<GroupAttractionCompatibility> snapshot,
        IReadOnlyCollection<GroupAttractionCompatibility> attractionCompatibilities,
        DateOnly evaluationDate)
    {
        GroupAttractionCompatibility first = snapshot.First();
        IReadOnlyCollection<string> expectedMemberKeys = first.Members
            .Select(static member => member.MemberKey)
            .OrderBy(static memberKey => memberKey, StringComparer.Ordinal)
            .ToList();
        foreach (GroupAttractionCompatibility compatibility in snapshot)
        {
            if (!string.Equals(
                    compatibility.MethodVersion,
                    GroupAttractionCompatibilityEvaluator.MethodVersion,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Group compatibilities must use the current methodology version.",
                    nameof(attractionCompatibilities));
            }

            if (compatibility.EvaluationDate != evaluationDate)
            {
                throw new ArgumentException(
                    "Group compatibilities must match the requested evaluation date.",
                    nameof(attractionCompatibilities));
            }

            IReadOnlyCollection<string> memberKeys = compatibility.Members
                .Select(static member => member.MemberKey)
                .OrderBy(static memberKey => memberKey, StringComparer.Ordinal)
                .ToList();
            if (!expectedMemberKeys.SequenceEqual(memberKeys, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    "Every attraction compatibility must contain the same members.",
                    nameof(attractionCompatibilities));
            }
        }
    }

    private static decimal? CalculateMinimumMemberValue(
        IReadOnlyCollection<GroupAttractionCompatibility> compatibilities)
    {
        IEnumerable<string> memberKeys = compatibilities.First().Members
            .Select(static member => member.MemberKey);
        decimal? minimumMemberValue = null;
        foreach (string memberKey in memberKeys)
        {
            List<AttractionCompatibilityState> knownStates = compatibilities
                .SelectMany(static compatibility => compatibility.Members)
                .Where(member => string.Equals(
                    member.MemberKey,
                    memberKey,
                    StringComparison.Ordinal))
                .Select(static member => member.Compatibility.State)
                .Where(static state => state is AttractionCompatibilityState.CompatibleAlone
                    or AttractionCompatibilityState.CompatibleWithCompanion
                    or AttractionCompatibilityState.Incompatible)
                .ToList();
            if (knownStates.Count == 0)
            {
                return null;
            }

            int compatibleCount = knownStates.Count(static state =>
                state is AttractionCompatibilityState.CompatibleAlone
                    or AttractionCompatibilityState.CompatibleWithCompanion);
            decimal memberValue = 100m * compatibleCount / knownStates.Count;
            minimumMemberValue = !minimumMemberValue.HasValue
                || memberValue < minimumMemberValue.Value
                    ? memberValue
                    : minimumMemberValue;
        }

        return minimumMemberValue;
    }

    private static decimal CalculateCoveragePercent(
        IReadOnlyCollection<GroupAttractionCompatibility> compatibilities,
        int knownGroupOutcomeCount)
    {
        decimal groupCoverage = 100m * knownGroupOutcomeCount / compatibilities.Count;
        IEnumerable<string> memberKeys = compatibilities.First().Members
            .Select(static member => member.MemberKey);
        decimal minimumMemberCoverage = memberKeys.Min(memberKey =>
        {
            int knownMemberOutcomes = compatibilities
                .SelectMany(static compatibility => compatibility.Members)
                .Count(member => string.Equals(
                        member.MemberKey,
                        memberKey,
                        StringComparison.Ordinal)
                    && member.Compatibility.State is AttractionCompatibilityState.CompatibleAlone
                        or AttractionCompatibilityState.CompatibleWithCompanion
                        or AttractionCompatibilityState.Incompatible);
            return 100m * knownMemberOutcomes / compatibilities.Count;
        });

        return Math.Min(groupCoverage, minimumMemberCoverage);
    }

    private static decimal GetGroupOutcomeValue(GroupAttractionCompatibilityState state)
    {
        return state switch
        {
            GroupAttractionCompatibilityState.EveryoneTogether => EveryoneTogetherValue,
            GroupAttractionCompatibilityState.PossibleWithSplit => PossibleWithSplitValue,
            GroupAttractionCompatibilityState.Partial => PartialValue,
            GroupAttractionCompatibilityState.None => NoneValue,
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };
    }

    private static ParkFitSubscore BuildUnknown(DateOnly evaluationDate)
    {
        return new ParkFitSubscore(
            ParkFitSubscoreKind.GroupCompatibility,
            ParkFitSubscoreState.Unknown,
            null,
            0m,
            ParkFitDataConfidence.Unknown,
            new[] { ParkFitSubscoreReasonCode.NoKnownFact },
            evaluationDate);
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
