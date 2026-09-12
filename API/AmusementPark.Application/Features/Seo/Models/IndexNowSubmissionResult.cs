namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Résultat d'une soumission IndexNow.
/// </summary>
public sealed class IndexNowSubmissionResult
{
    public bool WasRequested { get; init; }

    public bool IsEnabled { get; init; }

    public bool IsSuccess { get; init; }

    public int SubmittedUrlCount { get; init; }

    public IReadOnlyCollection<string> AcceptedEndpoints { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
