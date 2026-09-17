namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class CaptureWatchPilotInteractionRequestDto
{
    public string InteractionKind { get; init; } = string.Empty;

    public string? NotificationId { get; init; }
}
