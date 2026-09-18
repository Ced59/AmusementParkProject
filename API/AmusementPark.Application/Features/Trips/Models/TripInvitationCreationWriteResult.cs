namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripInvitationCreationWriteResult(
    TripInvitationCreationWriteOutcome Outcome,
    TripInvitationCreationRecord? Record = null);
