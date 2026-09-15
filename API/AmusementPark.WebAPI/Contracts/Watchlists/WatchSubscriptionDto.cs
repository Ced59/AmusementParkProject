namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class WatchSubscriptionDto
{
    public string SubscriptionId { get; init; } = string.Empty;

    public string TargetType { get; init; } = string.Empty;

    public string TargetId { get; init; } = string.Empty;

    public string? TargetName { get; init; }

    public string? ParentParkId { get; init; }

    public string? ParentParkName { get; init; }

    public string? MainImageId { get; init; }

    public IReadOnlyCollection<string> EventTypes { get; init; } = Array.Empty<string>();

    public string Frequency { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Channels { get; init; } = Array.Empty<string>();

    public bool IsPaused { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public long Version { get; init; }
}
