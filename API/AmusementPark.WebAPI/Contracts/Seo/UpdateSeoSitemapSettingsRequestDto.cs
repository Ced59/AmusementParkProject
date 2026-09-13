namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class UpdateSeoSitemapSettingsRequestDto
{
    public bool IsIndexNowEnabled { get; init; }

    public bool SubmitToIndexNowAfterManualGeneration { get; init; }

    public bool SubmitToIndexNowAfterAutomaticGeneration { get; init; }

    public string? IndexNowKey { get; init; }

    public string? IndexNowKeyLocation { get; init; }

    public IReadOnlyCollection<string> IndexNowEndpoints { get; init; } = Array.Empty<string>();
}
