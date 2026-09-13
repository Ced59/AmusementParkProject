namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class SeoSitemapSettingsDto
{
    public bool IsIndexNowEnabled { get; init; }

    public bool SubmitToIndexNowAfterManualGeneration { get; init; }

    public bool SubmitToIndexNowAfterAutomaticGeneration { get; init; }

    public string IndexNowKey { get; init; } = string.Empty;

    public string IndexNowKeyLocation { get; init; } = string.Empty;

    public IReadOnlyCollection<string> IndexNowEndpoints { get; init; } = Array.Empty<string>();

    public DateTime UpdatedAtUtc { get; init; }
}
