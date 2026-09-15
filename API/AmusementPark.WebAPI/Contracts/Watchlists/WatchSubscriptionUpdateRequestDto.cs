namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class WatchSubscriptionUpdateRequestDto
{
    public IReadOnlyCollection<string> EventTypes { get; init; } = Array.Empty<string>();

    public string Frequency { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Channels { get; init; } = Array.Empty<string>();

    public long ExpectedVersion { get; init; }
}
