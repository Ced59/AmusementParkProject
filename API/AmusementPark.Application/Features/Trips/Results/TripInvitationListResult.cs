namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripInvitationListResult(
    string InviterDisplayName,
    IReadOnlyCollection<TripInvitationSummaryResult> Invitations);
