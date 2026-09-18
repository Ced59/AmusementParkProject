using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripParticipantHttpMapper
{
    public static TripParticipantListDto ToHttp(this TripParticipantListResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripParticipantListDto
        {
            Participants = result.Participants.Select(static participant => new TripParticipantDto
            {
                MemberId = participant.MemberId,
                DisplayName = participant.DisplayName,
                Role = participant.Role.ToString(),
                IsCurrentUser = participant.IsCurrentUser,
                JoinedAtUtc = participant.JoinedAtUtc,
            }).ToArray(),
            CanManageRoles = result.CanManageRoles,
            CanTransferOwnership = result.CanTransferOwnership,
            CanLeave = result.CanLeave,
            TripVersion = result.TripVersion,
        };
    }

    public static TripInvitationDecisionDto ToHttp(this TripInvitationDecisionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripInvitationDecisionDto
        {
            TripPlanId = result.TripPlanId,
            WasReplayed = result.WasReplayed,
        };
    }
}
