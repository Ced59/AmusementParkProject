namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class WatchSubscriptionWriteRequestDto
{
    public string TargetType { get; init; } = string.Empty;

    public string TargetId { get; init; } = string.Empty;

    public IReadOnlyCollection<string> EventTypes { get; init; } = Array.Empty<string>();

    public string Frequency { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Channels { get; init; } = Array.Empty<string>();
}
