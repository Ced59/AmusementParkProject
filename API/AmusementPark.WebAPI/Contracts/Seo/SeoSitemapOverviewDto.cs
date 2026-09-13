namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class SeoSitemapOverviewDto
{
    public SeoSitemapRuntimeDto Runtime { get; init; } = new SeoSitemapRuntimeDto();

    public DateTime? LastGeneratedAtUtc { get; init; }

    public string PublicBaseUrl { get; init; } = string.Empty;

    public int TotalUrlCount { get; init; }

    public IReadOnlyCollection<SeoSitemapSectionStatsDto> Sections { get; init; } = Array.Empty<SeoSitemapSectionStatsDto>();

    public SeoSitemapSettingsDto Settings { get; init; } = new SeoSitemapSettingsDto();

    public string SitemapIndexUrl { get; init; } = string.Empty;

    public string RobotsUrl { get; init; } = string.Empty;

    public string IndexNowKeyFileUrl { get; init; } = string.Empty;

    public IReadOnlyCollection<string> PublicSitemapUrls { get; init; } = Array.Empty<string>();
}
