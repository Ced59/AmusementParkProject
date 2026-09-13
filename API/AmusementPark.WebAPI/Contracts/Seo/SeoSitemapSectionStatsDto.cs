namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class SeoSitemapSectionStatsDto
{
    public string Key { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public int UrlCount { get; init; }

    public DateTime? LastModifiedUtc { get; init; }

    public string PublicUrl { get; init; } = string.Empty;
}
