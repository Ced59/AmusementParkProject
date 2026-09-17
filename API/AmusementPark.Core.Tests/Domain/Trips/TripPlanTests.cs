using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripPlanTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 1, 2, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldBuildAPrivateDraftWithOneAuthoritativeOwner()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "  Été à Phantasialand  ",
            TripDateProposal.Fixed(new DateOnly(2027, 7, 8)),
            "Europe/Paris",
            CreatedAtUtc);

        Assert.Equal("Été à Phantasialand", trip.Title);
        Assert.Equal(TripPlanStatus.Draft, trip.Status);
        Assert.Equal(TripPlanAccessScope.MembersOnly, trip.AccessScope);
        TripMember owner = Assert.Single(trip.Members);
        Assert.Equal("user-1", owner.UserId);
        Assert.Equal(TripMembershipState.Active, owner.State);
        Assert.Null(owner.DelegatedRole);
        Assert.Equal(1, trip.Version);
        Assert.Equal(1, trip.ChildMutationEpoch);
    }

    [Fact]
    public void FineMutations_ShouldIncrementVersionOnlyWhenTheValueChanges()
    {
        TripDateProposal proposal = TripDateProposal.None();
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            proposal,
            null,
            CreatedAtUtc);

        trip.Rename("Voyage", CreatedAtUtc.AddMinutes(1));
        Assert.Equal(1, trip.Version);

        trip.SetDates(
            TripDateProposal.Fixed(new DateOnly(2027, 7, 8)),
            "Europe/Paris",
            CreatedAtUtc.AddMinutes(2));
        Assert.Equal(2, trip.Version);
        Assert.Equal(2, trip.ChildMutationEpoch);
        Assert.Equal(CreatedAtUtc.AddMinutes(2), trip.UpdatedAtUtc);

        trip.Rename("Voyage été", CreatedAtUtc.AddMinutes(3));
        Assert.Equal(3, trip.Version);
    }

    [Fact]
    public void Create_ShouldRequireATimeZoneWhenDatesAreDefined()
    {
        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() => TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.Fixed(new DateOnly(2027, 7, 8)),
            null,
            CreatedAtUtc));

        Assert.Equal(TripPlanErrorCodes.InvalidTimeZone, exception.Code);
    }

    [Fact]
    public void BeginDeletion_ShouldCloseAdmissionsAndIncrementTheVersion()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            CreatedAtUtc);

        trip.BeginDeletion(CreatedAtUtc.AddMinutes(1));

        Assert.Equal(TripDeletionState.Pending, trip.DeletionState);
        Assert.Equal(TripAdmissionClosureState.Closing, trip.AdmissionClosureState);
        Assert.Equal(2, trip.Version);
        Assert.Equal(2, trip.ChildMutationEpoch);
    }
}
