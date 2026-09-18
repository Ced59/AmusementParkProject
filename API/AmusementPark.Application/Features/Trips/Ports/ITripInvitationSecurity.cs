using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripInvitationSecurity
{
    string HashOperationKey(string actorUserId, string clientOperationId);

    string HashCreationPayload(
        TripDelegatedRole proposedRole,
        int lifetimeHours,
        string? normalizedTargetEmail);

    TripInvitationTokenMaterial CreateToken(
        TripInvitationId invitationId,
        string actorUserId,
        string operationKeyHash,
        string requestHash);

    bool TryRevealToken(TripInvitationCreationRecord record, string actorUserId, out string token);

    bool TryHashPublicToken(string token, out string tokenHash);

    TripInvitationEmailFingerprint FingerprintEmail(string normalizedEmail);
}
