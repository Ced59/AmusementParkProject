namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class NotificationEmailPreferenceUpdateRequestDto
{
    public bool EmailDigestEnabled { get; init; }

    public bool ConsentAccepted { get; init; }

    public string ConsentLocale { get; init; } = string.Empty;

    public long? ExpectedVersion { get; init; }
}
