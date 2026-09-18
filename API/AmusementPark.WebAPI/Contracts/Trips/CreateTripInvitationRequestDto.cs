using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class CreateTripInvitationRequestDto
{
    public long ExpectedPlanVersion { get; set; }

    public TripDelegatedRole ProposedRole { get; set; }

    public int LifetimeHours { get; set; }

    public string? TargetEmail { get; set; }

    public bool TryToApplication(out TripInvitationCreateInput? input)
    {
        input = null;
        if (this.ExpectedPlanVersion < 1
            || !Enum.IsDefined(this.ProposedRole)
            || this.LifetimeHours < TripInvitation.MinimumLifetime.TotalHours
            || this.LifetimeHours > TripInvitation.MaximumLifetime.TotalHours)
        {
            return false;
        }

        input = new TripInvitationCreateInput(
            this.ExpectedPlanVersion,
            this.ProposedRole,
            this.LifetimeHours,
            this.TargetEmail);
        return true;
    }
}
