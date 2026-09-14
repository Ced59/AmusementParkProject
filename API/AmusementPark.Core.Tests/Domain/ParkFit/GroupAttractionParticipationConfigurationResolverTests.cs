using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class GroupAttractionParticipationConfigurationResolverTests
{
    [Theory]
    [InlineData(AttractionCompatibilityState.CompatibleAlone)]
    [InlineData(AttractionCompatibilityState.CompatibleWithCompanion)]
    public void Resolve_WhenSingleMemberCanParticipate_ShouldReturnEveryoneTogether(
        AttractionCompatibilityState state)
    {
        GroupAttractionParticipationConfiguration result =
            GroupAttractionParticipationConfigurationResolver.Resolve(
                new[] { BuildMember("member-1", state) });

        Assert.Equal(GroupAttractionParticipationConfiguration.EveryoneTogether, result);
    }

    [Fact]
    public void Resolve_WhenSeveralMembersCanParticipate_ShouldNotAssumeVehicleCapacity()
    {
        GroupAttractionParticipationConfiguration result =
            GroupAttractionParticipationConfigurationResolver.Resolve(
                new[]
                {
                    BuildMember("member-1", AttractionCompatibilityState.CompatibleAlone),
                    BuildMember("member-2", AttractionCompatibilityState.CompatibleAlone),
                });

        Assert.Equal(GroupAttractionParticipationConfiguration.Unknown, result);
    }

    [Fact]
    public void Resolve_WhenGroupIsEmpty_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() =>
            GroupAttractionParticipationConfigurationResolver.Resolve(
                Array.Empty<GroupAttractionMemberCompatibility>()));
    }

    [Theory]
    [InlineData(AttractionCompatibilityState.Incompatible)]
    [InlineData(AttractionCompatibilityState.Unknown)]
    [InlineData(AttractionCompatibilityState.NotApplicable)]
    public void Resolve_WhenSingleMemberCannotBeConfirmed_ShouldReturnUnknown(
        AttractionCompatibilityState state)
    {
        GroupAttractionParticipationConfiguration result =
            GroupAttractionParticipationConfigurationResolver.Resolve(
                new[] { BuildMember("member-1", state) });

        Assert.Equal(GroupAttractionParticipationConfiguration.Unknown, result);
    }

    private static GroupAttractionMemberCompatibility BuildMember(
        string memberKey,
        AttractionCompatibilityState state)
    {
        return new GroupAttractionMemberCompatibility(
            memberKey,
            new AttractionCompatibility
            {
                MethodVersion = AttractionCompatibilityEvaluator.MethodVersion,
                State = state,
                Reasons = Array.Empty<AttractionCompatibilityReason>(),
                Confidence = state == AttractionCompatibilityState.Unknown
                    ? ParkFitDataConfidence.Unknown
                    : ParkFitDataConfidence.High,
                EvaluatedAtUtc = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc),
                EvaluationDate = new DateOnly(2026, 9, 14),
                Sources = Array.Empty<AttractionCompatibilitySourceReference>(),
            });
    }
}
