namespace AmusementPark.Core.Domain.Trips;

public static class TripAuthorizationPolicy
{
    public static bool HasPermission(
        TripEffectiveRole role,
        TripPermission permission,
        bool editorsCanInvite = false,
        bool participantsCanAddCandidates = false)
    {
        if (!Enum.IsDefined(role) || !Enum.IsDefined(permission))
        {
            return false;
        }

        return permission switch
        {
            TripPermission.Read or TripPermission.ManageOwnConstraints or TripPermission.Export => true,
            TripPermission.EditPlan or TripPermission.EditProgram =>
                role is TripEffectiveRole.Owner or TripEffectiveRole.Editor,
            TripPermission.AddCandidates => role is TripEffectiveRole.Owner or TripEffectiveRole.Editor
                || (role == TripEffectiveRole.Participant && participantsCanAddCandidates),
            TripPermission.Invite => role == TripEffectiveRole.Owner
                || (role == TripEffectiveRole.Editor && editorsCanInvite),
            TripPermission.ChangeRoles or TripPermission.Delete => role == TripEffectiveRole.Owner,
            TripPermission.Vote => role is TripEffectiveRole.Owner
                or TripEffectiveRole.Editor
                or TripEffectiveRole.Participant,
            TripPermission.Leave => role != TripEffectiveRole.Owner,
            _ => false,
        };
    }
}
