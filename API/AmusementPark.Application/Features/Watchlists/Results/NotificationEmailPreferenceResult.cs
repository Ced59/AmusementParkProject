namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record NotificationEmailPreferenceResult(
    bool EmailDigestEnabled,
    bool EmailAvailable,
    string? MaskedEmail,
    string ConsentTextVersion,
    DateTime? ConsentGrantedAtUtc,
    DateTime? RevokedAtUtc,
    long? Version);
