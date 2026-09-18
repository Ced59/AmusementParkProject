namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripInvitationEmailFingerprint(string Hmac, string KeyVersion);
