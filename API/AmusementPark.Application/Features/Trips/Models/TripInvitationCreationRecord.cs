using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripInvitationCreationRecord(
    TripInvitation Invitation,
    string OperationKeyHash,
    string RequestHash,
    string SealedToken,
    string SealedTokenKeyVersion,
    bool WasReplayed);
