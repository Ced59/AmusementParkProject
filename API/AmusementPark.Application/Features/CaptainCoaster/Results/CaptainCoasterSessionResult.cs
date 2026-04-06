namespace AmusementPark.Application.Features.CaptainCoaster.Results;

/// <summary>
/// Représente l'état applicatif d'une session Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSessionResult
{
    public string SessionId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int ProgressPercentage { get; init; }

    public string? Message { get; init; }
}
