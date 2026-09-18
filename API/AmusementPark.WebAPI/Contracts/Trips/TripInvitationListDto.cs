namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationListDto(
    string InviterDisplayName,
    IReadOnlyCollection<TripInvitationSummaryDto> Invitations);
