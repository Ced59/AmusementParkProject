using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripPlanCollaborationTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 1, 2, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Admission_ShouldHideAProvisionalMemberUntilEstablishment()
    {
        TripPlan trip = CreateTrip();
        TripInvitationId invitationId = TripInvitationId.New();

        trip.PrepareAdmission(
            invitationId,
            "operation-1",
            "user-2",
            1,
            CreatedAtUtc.AddHours(1),
            CreatedAtUtc.AddMinutes(1));
        trip.ArmAdmission("operation-1", 1);
        trip.ApplyAdmission(
            "operation-1",
            1,
            TripDelegatedRole.Participant,
            CreatedAtUtc.AddMinutes(2));

        TripMember provisional = Assert.Single(
            trip.Members,
            member => string.Equals(member.UserId, "user-2", StringComparison.Ordinal));
        Assert.Equal(TripMembershipState.Provisional, provisional.State);
        Assert.Null(trip.ResolveRole("user-2"));

        trip.EstablishAdmission("operation-1", 1, CreatedAtUtc.AddMinutes(3));

        Assert.Equal(TripMembershipState.Active, provisional.State);
        Assert.Equal("operation-1", provisional.AdmissionOperationId);
        Assert.Equal(TripEffectiveRole.Participant, trip.ResolveRole("user-2"));
        Assert.Null(trip.MemberAdmissionFence);
    }

    [Fact]
    public void TransferOwnership_ShouldDemoteThePreviousOwnerAndPromoteTheTarget()
    {
        TripPlan trip = CreateTripWithEstablishedMember("user-2", TripDelegatedRole.Editor);
        TripMember newOwner = trip.Members.Single(member => member.UserId == "user-2");

        trip.TransferOwnership(
            "user-1",
            newOwner.Id,
            TripDelegatedRole.Participant,
            CreatedAtUtc.AddMinutes(4));

        Assert.Equal("user-2", trip.OwnerUserId);
        Assert.Equal(TripEffectiveRole.Owner, trip.ResolveRole("user-2"));
        Assert.Equal(TripEffectiveRole.Participant, trip.ResolveRole("user-1"));
    }

    [Fact]
    public void Owner_ShouldHaveToTransferOwnershipBeforeLeaving()
    {
        TripPlan trip = CreateTripWithEstablishedMember("user-2", TripDelegatedRole.Viewer);

        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            trip.BeginMemberDeparture("user-1", CreatedAtUtc.AddMinutes(4)));

        Assert.Equal(TripPlanErrorCodes.InvalidOwner, exception.Code);
    }

    [Fact]
    public void MemberDeparture_ShouldRemoveOnlyTheLeavingMember()
    {
        TripPlan trip = CreateTripWithEstablishedMember("user-2", TripDelegatedRole.Viewer);

        TripMember leaving = trip.BeginMemberDeparture("user-2", CreatedAtUtc.AddMinutes(4));
        trip.RemoveLeavingMember(leaving.Id, CreatedAtUtc.AddMinutes(5));

        Assert.DoesNotContain(trip.Members, member => member.UserId == "user-2");
        Assert.Equal(TripEffectiveRole.Owner, trip.ResolveRole("user-1"));
    }

    private static TripPlan CreateTripWithEstablishedMember(string userId, TripDelegatedRole role)
    {
        TripPlan trip = CreateTrip();
        trip.PrepareAdmission(
            TripInvitationId.New(),
            "operation-1",
            userId,
            1,
            CreatedAtUtc.AddHours(1),
            CreatedAtUtc.AddMinutes(1));
        trip.ArmAdmission("operation-1", 1);
        trip.ApplyAdmission("operation-1", 1, role, CreatedAtUtc.AddMinutes(2));
        trip.EstablishAdmission("operation-1", 1, CreatedAtUtc.AddMinutes(3));
        return trip;
    }

    private static TripPlan CreateTrip()
    {
        return TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            CreatedAtUtc);
    }
}
