namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record NotificationEmailPreferenceInput(
    bool EmailDigestEnabled,
    bool ConsentAccepted,
    string ConsentLocale,
    long? ExpectedVersion);
