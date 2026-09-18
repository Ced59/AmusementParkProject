namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripInvitationTokenMaterial(
    string PlainTextToken,
    string TokenHash,
    string TokenHint,
    string SealedToken,
    string KeyVersion);
