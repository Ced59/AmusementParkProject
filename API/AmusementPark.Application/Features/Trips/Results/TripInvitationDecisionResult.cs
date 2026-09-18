namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripInvitationDecisionResult(
    string TripPlanId,
    bool WasReplayed);
