namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripParticipantListResult(
    IReadOnlyCollection<TripParticipantResult> Participants,
    bool CanManageRoles,
    bool CanTransferOwnership,
    bool CanLeave,
    long TripVersion);
