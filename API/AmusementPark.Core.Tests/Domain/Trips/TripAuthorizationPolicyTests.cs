using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripAuthorizationPolicyTests
{
    [Theory]
    [InlineData(TripEffectiveRole.Owner, true, true, true, true, true)]
    [InlineData(TripEffectiveRole.Editor, true, true, true, false, true)]
    [InlineData(TripEffectiveRole.Participant, false, false, false, false, true)]
    [InlineData(TripEffectiveRole.Viewer, false, false, false, false, false)]
    public void HasPermission_ShouldApplyTheRoleMatrix(
        TripEffectiveRole role,
        bool canEditPlan,
        bool canEditProgram,
        bool canAddCandidates,
        bool canManageRoles,
        bool canVote)
    {
        Assert.Equal(canEditPlan, TripAuthorizationPolicy.HasPermission(role, TripPermission.EditPlan));
        Assert.Equal(canEditProgram, TripAuthorizationPolicy.HasPermission(role, TripPermission.EditProgram));
        Assert.Equal(canAddCandidates, TripAuthorizationPolicy.HasPermission(role, TripPermission.AddCandidates));
        Assert.Equal(canManageRoles, TripAuthorizationPolicy.HasPermission(role, TripPermission.ChangeRoles));
        Assert.Equal(canVote, TripAuthorizationPolicy.HasPermission(role, TripPermission.Vote));
        Assert.True(TripAuthorizationPolicy.HasPermission(role, TripPermission.Read));
    }

    [Fact]
    public void HasPermission_ShouldKeepOptionalDelegationsExplicit()
    {
        Assert.False(TripAuthorizationPolicy.HasPermission(TripEffectiveRole.Editor, TripPermission.Invite));
        Assert.True(TripAuthorizationPolicy.HasPermission(
            TripEffectiveRole.Editor,
            TripPermission.Invite,
            editorsCanInvite: true));
        Assert.False(TripAuthorizationPolicy.HasPermission(
            TripEffectiveRole.Participant,
            TripPermission.AddCandidates));
        Assert.True(TripAuthorizationPolicy.HasPermission(
            TripEffectiveRole.Participant,
            TripPermission.AddCandidates,
            participantsCanAddCandidates: true));
    }
}
