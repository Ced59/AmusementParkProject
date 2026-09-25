namespace AmusementPark.Core.Domain.Trips;

public static class TripNotificationPolicy
{
    public const int MaximumUnreadCount = 99;

    public static readonly IReadOnlyCollection<TripActivityKind> ImportantActivityKinds =
        new[]
        {
            TripActivityKind.TripRenamed,
            TripActivityKind.DatesChanged,
            TripActivityKind.CandidateAdded,
            TripActivityKind.CandidateUpdated,
            TripActivityKind.CandidateMoved,
            TripActivityKind.CandidateRemoved,
            TripActivityKind.DayUpdated,
            TripActivityKind.DayRemoved,
            TripActivityKind.InvitationCreated,
            TripActivityKind.InvitationRevoked,
            TripActivityKind.InvitationAccepted,
            TripActivityKind.InvitationDeclined,
            TripActivityKind.ParticipantRoleChanged,
            TripActivityKind.OwnershipTransferred,
            TripActivityKind.ParticipantLeft,
            TripActivityKind.PreferencesUpdated,
            TripActivityKind.CollectiveDecisionUpdated,
        };

    public static bool IsImportant(TripActivityKind kind)
    {
        return ImportantActivityKinds.Contains(kind);
    }
}
