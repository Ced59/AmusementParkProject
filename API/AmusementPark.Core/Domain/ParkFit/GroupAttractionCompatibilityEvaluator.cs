namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Agrège les verdicts individuels sans supposer une capacité ou un accompagnement.
/// </summary>
public sealed class GroupAttractionCompatibilityEvaluator
{
    public const string MethodVersion = AttractionCompatibilityEvaluator.MethodVersion;

    public GroupAttractionCompatibility Evaluate(
        IReadOnlyCollection<GroupAttractionMemberCompatibility> members,
        GroupAttractionParticipationConfiguration participationConfiguration)
    {
        ArgumentNullException.ThrowIfNull(members);

        if (members.Count == 0)
        {
            throw new ArgumentException(
                "At least one member compatibility is required.",
                nameof(members));
        }

        if (!Enum.IsDefined(participationConfiguration))
        {
            throw new ArgumentOutOfRangeException(nameof(participationConfiguration));
        }

        List<GroupAttractionMemberCompatibility> snapshot = members.ToList();
        if (snapshot.Any(static member => member is null))
        {
            throw new ArgumentException(
                "Member compatibilities cannot contain null entries.",
                nameof(members));
        }

        ValidateMemberKeys(snapshot, members);
        ValidateIndividualResults(snapshot, members);

        int compatibleAloneCount = CountState(
            snapshot,
            AttractionCompatibilityState.CompatibleAlone);
        int compatibleWithCompanionCount = CountState(
            snapshot,
            AttractionCompatibilityState.CompatibleWithCompanion);
        int incompatibleCount = CountState(
            snapshot,
            AttractionCompatibilityState.Incompatible);
        int unknownCount = CountState(snapshot, AttractionCompatibilityState.Unknown);
        int notApplicableCount = CountState(
            snapshot,
            AttractionCompatibilityState.NotApplicable);
        int compatibleCount = compatibleAloneCount + compatibleWithCompanionCount;
        bool everyMemberIsCompatible = compatibleCount == snapshot.Count;

        if (!everyMemberIsCompatible
            && participationConfiguration
                != GroupAttractionParticipationConfiguration.Unknown)
        {
            throw new ArgumentException(
                "A known participation configuration requires every member to be compatible.",
                nameof(participationConfiguration));
        }

        GroupAttractionCompatibilityState state = DetermineState(
            snapshot.Count,
            compatibleCount,
            incompatibleCount,
            participationConfiguration);
        IReadOnlyCollection<GroupAttractionCompatibilityReasonCode> reasons = BuildReasons(
            state,
            unknownCount,
            notApplicableCount);
        IOrderedEnumerable<GroupAttractionMemberCompatibility> orderedMembers = snapshot
            .OrderBy(static member => member.MemberKey, StringComparer.Ordinal);
        AttractionCompatibility firstCompatibility = snapshot[0].Compatibility;

        return new GroupAttractionCompatibility(
            MethodVersion,
            state,
            participationConfiguration,
            orderedMembers.ToList(),
            reasons,
            snapshot.Min(static member => member.Compatibility.Confidence),
            firstCompatibility.EvaluationDate,
            snapshot.Max(static member => member.Compatibility.EvaluatedAtUtc),
            compatibleAloneCount,
            compatibleWithCompanionCount,
            incompatibleCount,
            unknownCount,
            notApplicableCount);
    }

    private static void ValidateMemberKeys(
        IEnumerable<GroupAttractionMemberCompatibility> snapshot,
        IReadOnlyCollection<GroupAttractionMemberCompatibility> members)
    {
        HashSet<string> memberKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (GroupAttractionMemberCompatibility member in snapshot)
        {
            if (!memberKeys.Add(member.MemberKey))
            {
                throw new ArgumentException(
                    "Member keys must be unique within one group evaluation.",
                    nameof(members));
            }
        }
    }

    private static void ValidateIndividualResults(
        IEnumerable<GroupAttractionMemberCompatibility> snapshot,
        IReadOnlyCollection<GroupAttractionMemberCompatibility> members)
    {
        DateOnly? evaluationDate = null;
        foreach (GroupAttractionMemberCompatibility member in snapshot)
        {
            AttractionCompatibility compatibility = member.Compatibility;
            if (!Enum.IsDefined(compatibility.State))
            {
                throw new ArgumentException(
                    "An individual compatibility state is invalid.",
                    nameof(members));
            }

            if (!Enum.IsDefined(compatibility.Confidence))
            {
                throw new ArgumentException(
                    "An individual data confidence is invalid.",
                    nameof(members));
            }

            if (!string.Equals(
                    compatibility.MethodVersion,
                    MethodVersion,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Individual compatibilities must use the current methodology version.",
                    nameof(members));
            }

            if (compatibility.EvaluatedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Individual evaluation timestamps must use UTC.",
                    nameof(members));
            }

            if (evaluationDate.HasValue
                && compatibility.EvaluationDate != evaluationDate.Value)
            {
                throw new ArgumentException(
                    "Individual compatibilities must share one evaluation date.",
                    nameof(members));
            }

            evaluationDate = compatibility.EvaluationDate;
        }
    }

    private static int CountState(
        IEnumerable<GroupAttractionMemberCompatibility> members,
        AttractionCompatibilityState state)
    {
        return members.Count(member => member.Compatibility.State == state);
    }

    private static GroupAttractionCompatibilityState DetermineState(
        int memberCount,
        int compatibleCount,
        int incompatibleCount,
        GroupAttractionParticipationConfiguration participationConfiguration)
    {
        if (compatibleCount == memberCount)
        {
            return participationConfiguration switch
            {
                GroupAttractionParticipationConfiguration.EveryoneTogether =>
                    GroupAttractionCompatibilityState.EveryoneTogether,
                GroupAttractionParticipationConfiguration.SplitRequired =>
                    GroupAttractionCompatibilityState.PossibleWithSplit,
                _ => GroupAttractionCompatibilityState.Unknown,
            };
        }

        if (compatibleCount > 0 && incompatibleCount > 0)
        {
            return GroupAttractionCompatibilityState.Partial;
        }

        if (incompatibleCount == memberCount)
        {
            return GroupAttractionCompatibilityState.None;
        }

        return GroupAttractionCompatibilityState.Unknown;
    }

    private static IReadOnlyCollection<GroupAttractionCompatibilityReasonCode> BuildReasons(
        GroupAttractionCompatibilityState state,
        int unknownCount,
        int notApplicableCount)
    {
        List<GroupAttractionCompatibilityReasonCode> reasons =
            new List<GroupAttractionCompatibilityReasonCode>();
        reasons.Add(state switch
        {
            GroupAttractionCompatibilityState.EveryoneTogether =>
                GroupAttractionCompatibilityReasonCode.EveryoneTogetherKnown,
            GroupAttractionCompatibilityState.PossibleWithSplit =>
                GroupAttractionCompatibilityReasonCode.SplitRequiredKnown,
            GroupAttractionCompatibilityState.Partial =>
                GroupAttractionCompatibilityReasonCode.PartialParticipation,
            GroupAttractionCompatibilityState.None =>
                GroupAttractionCompatibilityReasonCode.NoMemberCompatible,
            _ when unknownCount > 0 =>
                GroupAttractionCompatibilityReasonCode.MemberCompatibilityUnknown,
            _ when notApplicableCount > 0 =>
                GroupAttractionCompatibilityReasonCode.MemberCompatibilityNotApplicable,
            _ => GroupAttractionCompatibilityReasonCode.ParticipationConfigurationUnknown,
        });

        AddReasonWhenMissing(
            reasons,
            unknownCount > 0,
            GroupAttractionCompatibilityReasonCode.MemberCompatibilityUnknown);
        AddReasonWhenMissing(
            reasons,
            notApplicableCount > 0,
            GroupAttractionCompatibilityReasonCode.MemberCompatibilityNotApplicable);

        return reasons;
    }

    private static void AddReasonWhenMissing(
        ICollection<GroupAttractionCompatibilityReasonCode> reasons,
        bool shouldAdd,
        GroupAttractionCompatibilityReasonCode reason)
    {
        if (shouldAdd && !reasons.Contains(reason))
        {
            reasons.Add(reason);
        }
    }
}
