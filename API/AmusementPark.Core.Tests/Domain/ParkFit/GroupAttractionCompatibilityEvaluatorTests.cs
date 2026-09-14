using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class GroupAttractionCompatibilityEvaluatorTests
{
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 9, 14);
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly GroupAttractionCompatibilityEvaluator evaluator =
        new GroupAttractionCompatibilityEvaluator();

    [Fact]
    public void Evaluate_WhenEveryoneCanUseOneKnownConfiguration_ShouldReturnEveryoneTogether()
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.EveryoneTogether,
            BuildMember("child", AttractionCompatibilityState.CompatibleWithCompanion),
            BuildMember("adult", AttractionCompatibilityState.CompatibleAlone));

        Assert.Equal(GroupAttractionCompatibilityState.EveryoneTogether, result.State);
        Assert.Equal(
            GroupAttractionCompatibilityReasonCode.EveryoneTogetherKnown,
            Assert.Single(result.Reasons));
        Assert.Equal(1, result.CompatibleAloneCount);
        Assert.Equal(1, result.CompatibleWithCompanionCount);
        Assert.Equal(new[] { "adult", "child" }, result.Members.Select(static member => member.MemberKey));
    }

    [Fact]
    public void Evaluate_WhenEveryoneIsCompatibleButMustSplit_ShouldReturnPossibleWithSplit()
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.SplitRequired,
            BuildMember("adult", AttractionCompatibilityState.CompatibleAlone),
            BuildMember("child", AttractionCompatibilityState.CompatibleWithCompanion));

        Assert.Equal(GroupAttractionCompatibilityState.PossibleWithSplit, result.State);
        Assert.Equal(
            GroupAttractionCompatibilityReasonCode.SplitRequiredKnown,
            Assert.Single(result.Reasons));
    }

    [Fact]
    public void Evaluate_WhenEveryoneIsCompatibleButConfigurationIsUnknown_ShouldRemainUnknown()
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            BuildMember("adult", AttractionCompatibilityState.CompatibleAlone),
            BuildMember("child", AttractionCompatibilityState.CompatibleWithCompanion));

        Assert.Equal(GroupAttractionCompatibilityState.Unknown, result.State);
        Assert.Equal(
            GroupAttractionCompatibilityReasonCode.ParticipationConfigurationUnknown,
            Assert.Single(result.Reasons));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Evaluate_WhenCompatibleAndIncompatibleMembersExist_ShouldReturnPartial(
        bool includeUnknownMember)
    {
        List<GroupAttractionMemberCompatibility> members = new List<GroupAttractionMemberCompatibility>
        {
            BuildMember("compatible", AttractionCompatibilityState.CompatibleAlone),
            BuildMember("incompatible", AttractionCompatibilityState.Incompatible),
        };
        if (includeUnknownMember)
        {
            members.Add(BuildMember("unknown", AttractionCompatibilityState.Unknown));
        }

        GroupAttractionCompatibility result = this.evaluator.Evaluate(
            members,
            GroupAttractionParticipationConfiguration.Unknown);

        Assert.Equal(GroupAttractionCompatibilityState.Partial, result.State);
        Assert.Contains(
            GroupAttractionCompatibilityReasonCode.PartialParticipation,
            result.Reasons);
        Assert.Equal(includeUnknownMember ? 1 : 0, result.UnknownCount);
    }

    [Fact]
    public void Evaluate_WhenEveryMemberIsIncompatible_ShouldReturnNone()
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            BuildMember("first", AttractionCompatibilityState.Incompatible),
            BuildMember("second", AttractionCompatibilityState.Incompatible));

        Assert.Equal(GroupAttractionCompatibilityState.None, result.State);
        Assert.Equal(2, result.IncompatibleCount);
        Assert.Equal(
            GroupAttractionCompatibilityReasonCode.NoMemberCompatible,
            Assert.Single(result.Reasons));
    }

    [Theory]
    [InlineData(
        AttractionCompatibilityState.Incompatible,
        AttractionCompatibilityState.Unknown)]
    [InlineData(
        AttractionCompatibilityState.CompatibleAlone,
        AttractionCompatibilityState.Unknown)]
    [InlineData(
        AttractionCompatibilityState.Unknown,
        AttractionCompatibilityState.Unknown)]
    public void Evaluate_WhenAnUnknownMemberPreventsAConclusiveState_ShouldReturnUnknown(
        AttractionCompatibilityState firstState,
        AttractionCompatibilityState secondState)
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            BuildMember("first", firstState),
            BuildMember("second", secondState));

        Assert.Equal(GroupAttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            GroupAttractionCompatibilityReasonCode.MemberCompatibilityUnknown,
            result.Reasons);
    }

    [Theory]
    [InlineData(
        AttractionCompatibilityState.NotApplicable,
        AttractionCompatibilityState.NotApplicable)]
    [InlineData(
        AttractionCompatibilityState.CompatibleAlone,
        AttractionCompatibilityState.NotApplicable)]
    [InlineData(
        AttractionCompatibilityState.Incompatible,
        AttractionCompatibilityState.NotApplicable)]
    public void Evaluate_WhenNotApplicableMemberPreventsAGroupConclusion_ShouldReturnUnknown(
        AttractionCompatibilityState firstState,
        AttractionCompatibilityState secondState)
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            BuildMember("first", firstState),
            BuildMember("second", secondState));

        Assert.Equal(GroupAttractionCompatibilityState.Unknown, result.State);
        Assert.Contains(
            GroupAttractionCompatibilityReasonCode.MemberCompatibilityNotApplicable,
            result.Reasons);
    }

    [Fact]
    public void Evaluate_ShouldKeepUnknownAndNotApplicableReasonsVisibleInAPartialResult()
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            BuildMember("compatible", AttractionCompatibilityState.CompatibleAlone),
            BuildMember("incompatible", AttractionCompatibilityState.Incompatible),
            BuildMember("unknown", AttractionCompatibilityState.Unknown),
            BuildMember("not-applicable", AttractionCompatibilityState.NotApplicable));

        Assert.Equal(GroupAttractionCompatibilityState.Partial, result.State);
        Assert.Equal(
            new[]
            {
                GroupAttractionCompatibilityReasonCode.PartialParticipation,
                GroupAttractionCompatibilityReasonCode.MemberCompatibilityUnknown,
                GroupAttractionCompatibilityReasonCode.MemberCompatibilityNotApplicable,
            },
            result.Reasons);
    }

    [Fact]
    public void Evaluate_ShouldUseTheLowestIndividualConfidence()
    {
        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.EveryoneTogether,
            BuildMember(
                "high",
                AttractionCompatibilityState.CompatibleAlone,
                ParkFitDataConfidence.High),
            BuildMember(
                "low",
                AttractionCompatibilityState.CompatibleAlone,
                ParkFitDataConfidence.Low));

        Assert.Equal(ParkFitDataConfidence.Low, result.Confidence);
    }

    [Fact]
    public void Evaluate_ShouldExposeTheLatestIndividualEvaluationTimestamp()
    {
        DateTime laterTimestamp = EvaluationTimestamp.AddMinutes(5);

        GroupAttractionCompatibility result = this.Evaluate(
            GroupAttractionParticipationConfiguration.EveryoneTogether,
            BuildMember("earlier", AttractionCompatibilityState.CompatibleAlone),
            BuildMember(
                "later",
                AttractionCompatibilityState.CompatibleAlone,
                evaluatedAtUtc: laterTimestamp));

        Assert.Equal(EvaluationDate, result.EvaluationDate);
        Assert.Equal(laterTimestamp, result.EvaluatedAtUtc);
        Assert.Equal(GroupAttractionCompatibilityEvaluator.MethodVersion, result.MethodVersion);
    }

    [Fact]
    public void Evaluate_ShouldBeInvariantToMemberOrder()
    {
        GroupAttractionMemberCompatibility compatible = BuildMember(
            "b",
            AttractionCompatibilityState.CompatibleAlone);
        GroupAttractionMemberCompatibility incompatible = BuildMember(
            "a",
            AttractionCompatibilityState.Incompatible);
        GroupAttractionMemberCompatibility unknown = BuildMember(
            "c",
            AttractionCompatibilityState.Unknown);

        GroupAttractionCompatibility first = this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            compatible,
            incompatible,
            unknown);
        GroupAttractionCompatibility second = this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            unknown,
            compatible,
            incompatible);

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Reasons, second.Reasons);
        Assert.Equal(
            first.Members.Select(static member => member.MemberKey),
            second.Members.Select(static member => member.MemberKey));
    }

    [Fact]
    public void Evaluate_ShouldSnapshotTheMemberCollection()
    {
        List<GroupAttractionMemberCompatibility> members = new List<GroupAttractionMemberCompatibility>
        {
            BuildMember("member", AttractionCompatibilityState.CompatibleAlone),
        };

        GroupAttractionCompatibility result = this.evaluator.Evaluate(
            members,
            GroupAttractionParticipationConfiguration.EveryoneTogether);
        members.Clear();

        Assert.Single(result.Members);
    }

    [Fact]
    public void Evaluate_WhenThereIsNoMember_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            Array.Empty<GroupAttractionMemberCompatibility>(),
            GroupAttractionParticipationConfiguration.Unknown));
    }

    [Fact]
    public void Evaluate_WhenMemberCollectionIsNull_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentNullException>(() => this.evaluator.Evaluate(
            null!,
            GroupAttractionParticipationConfiguration.Unknown));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MemberCompatibility_WhenKeyIsBlank_ShouldRejectIt(string memberKey)
    {
        Assert.Throws<ArgumentException>(() => new GroupAttractionMemberCompatibility(
            memberKey,
            BuildCompatibility(AttractionCompatibilityState.CompatibleAlone)));
    }

    [Fact]
    public void MemberCompatibility_WhenResultIsNull_ShouldRejectIt()
    {
        Assert.Throws<ArgumentNullException>(() => new GroupAttractionMemberCompatibility(
            "member",
            null!));
    }

    [Fact]
    public void Evaluate_WhenTrimmedMemberKeysAreDuplicated_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            BuildMember("member", AttractionCompatibilityState.Unknown),
            BuildMember(" member ", AttractionCompatibilityState.Unknown)));
    }

    [Fact]
    public void Evaluate_WhenKnownConfigurationContradictsAMemberVerdict_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.Evaluate(
            GroupAttractionParticipationConfiguration.EveryoneTogether,
            BuildMember("compatible", AttractionCompatibilityState.CompatibleAlone),
            BuildMember("incompatible", AttractionCompatibilityState.Incompatible)));
    }

    [Fact]
    public void Evaluate_WhenParticipationConfigurationIsInvalid_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => this.Evaluate(
            (GroupAttractionParticipationConfiguration)99,
            BuildMember("member", AttractionCompatibilityState.CompatibleAlone)));
    }

    [Fact]
    public void Evaluate_WhenIndividualStateIsInvalid_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.Evaluate(
            GroupAttractionParticipationConfiguration.Unknown,
            BuildMember("member", (AttractionCompatibilityState)99)));
    }

    [Fact]
    public void Evaluate_WhenIndividualConfidenceIsInvalid_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new[]
            {
                new GroupAttractionMemberCompatibility(
                    "member",
                    BuildCompatibility(
                        AttractionCompatibilityState.Unknown,
                        (ParkFitDataConfidence)99)),
            },
            GroupAttractionParticipationConfiguration.Unknown));
    }

    [Fact]
    public void Evaluate_WhenIndividualMethodVersionDiffers_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new[]
            {
                new GroupAttractionMemberCompatibility(
                    "member",
                    BuildCompatibility(
                        AttractionCompatibilityState.Unknown,
                        methodVersion: "park-fit-other")),
            },
            GroupAttractionParticipationConfiguration.Unknown));
    }

    [Fact]
    public void Evaluate_WhenIndividualDatesDiffer_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new[]
            {
                BuildMember("first", AttractionCompatibilityState.Unknown),
                new GroupAttractionMemberCompatibility(
                    "second",
                    BuildCompatibility(
                        AttractionCompatibilityState.Unknown,
                        evaluationDate: EvaluationDate.AddDays(1))),
            },
            GroupAttractionParticipationConfiguration.Unknown));
    }

    [Fact]
    public void Evaluate_WhenIndividualTimestampIsNotUtc_ShouldRejectTheRequest()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new[]
            {
                new GroupAttractionMemberCompatibility(
                    "member",
                    BuildCompatibility(
                        AttractionCompatibilityState.Unknown,
                        evaluatedAtUtc: DateTime.SpecifyKind(
                            EvaluationTimestamp,
                            DateTimeKind.Local))),
            },
            GroupAttractionParticipationConfiguration.Unknown));
    }

    private GroupAttractionCompatibility Evaluate(
        GroupAttractionParticipationConfiguration participationConfiguration,
        params GroupAttractionMemberCompatibility[] members)
    {
        return this.evaluator.Evaluate(members, participationConfiguration);
    }

    private static GroupAttractionMemberCompatibility BuildMember(
        string memberKey,
        AttractionCompatibilityState state,
        ParkFitDataConfidence confidence = ParkFitDataConfidence.High,
        DateTime? evaluatedAtUtc = null)
    {
        return new GroupAttractionMemberCompatibility(
            memberKey,
            BuildCompatibility(state, confidence, evaluatedAtUtc: evaluatedAtUtc));
    }

    private static AttractionCompatibility BuildCompatibility(
        AttractionCompatibilityState state,
        ParkFitDataConfidence confidence = ParkFitDataConfidence.High,
        string methodVersion = GroupAttractionCompatibilityEvaluator.MethodVersion,
        DateOnly? evaluationDate = null,
        DateTime? evaluatedAtUtc = null)
    {
        return new AttractionCompatibility
        {
            MethodVersion = methodVersion,
            State = state,
            Reasons = Array.Empty<AttractionCompatibilityReason>(),
            Confidence = confidence,
            EvaluatedAtUtc = evaluatedAtUtc ?? EvaluationTimestamp,
            EvaluationDate = evaluationDate ?? EvaluationDate,
            Sources = Array.Empty<AttractionCompatibilitySourceReference>(),
        };
    }
}
