using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class CreateTripInvitationRequestDto
{
    public long ExpectedPlanVersion { get; set; }

    public TripDelegatedRoleDto ProposedRole { get; set; }

    public int LifetimeHours { get; set; }

    public string? TargetEmail { get; set; }

    public bool TryToApplication(out TripInvitationCreateInput? input)
    {
        input = null;
        if (this.ExpectedPlanVersion < 1
            || !TryMapRole(this.ProposedRole, out TripDelegatedRole proposedRole)
            || this.LifetimeHours < TripInvitation.MinimumLifetime.TotalHours
            || this.LifetimeHours > TripInvitation.MaximumLifetime.TotalHours)
        {
            return false;
        }

        input = new TripInvitationCreateInput(
            this.ExpectedPlanVersion,
            proposedRole,
            this.LifetimeHours,
            this.TargetEmail);
        return true;
    }

    private static bool TryMapRole(TripDelegatedRoleDto value, out TripDelegatedRole role)
    {
        role = value switch
        {
            TripDelegatedRoleDto.Editor => TripDelegatedRole.Editor,
            TripDelegatedRoleDto.Participant => TripDelegatedRole.Participant,
            TripDelegatedRoleDto.Viewer => TripDelegatedRole.Viewer,
            _ => default,
        };
        return Enum.IsDefined(role);
    }
}
