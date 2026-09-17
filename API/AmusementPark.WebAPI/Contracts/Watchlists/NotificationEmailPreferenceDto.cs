namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed record NotificationEmailPreferenceDto(
    bool EmailDigestEnabled,
    bool EmailAvailable,
    string? MaskedEmail,
    string ConsentTextVersion,
    DateTime? ConsentGrantedAtUtc,
    DateTime? RevokedAtUtc,
    long? Version);
