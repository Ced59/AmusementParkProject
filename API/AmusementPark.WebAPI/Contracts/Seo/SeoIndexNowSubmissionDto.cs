namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class SeoIndexNowSubmissionDto
{
    public bool WasRequested { get; init; }

    public bool IsEnabled { get; init; }

    public bool IsSuccess { get; init; }

    public int SubmittedUrlCount { get; init; }

    public IReadOnlyCollection<string> AcceptedEndpoints { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
