using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Results;

public sealed class SeoSitemapOverviewResult
{
    public SitemapRuntimeState Runtime { get; init; } = new SitemapRuntimeState();

    public SitemapSnapshot? Snapshot { get; init; }

    public SeoSitemapSettings Settings { get; init; } = new SeoSitemapSettings();

    public IReadOnlyCollection<SitemapSectionStats> Sections { get; init; } = Array.Empty<SitemapSectionStats>();

    public int TotalUrlCount { get; init; }

    public IReadOnlyCollection<string> PublicSitemapUrls { get; init; } = Array.Empty<string>();

    public string SitemapIndexUrl { get; init; } = string.Empty;

    public string RobotsUrl { get; init; } = string.Empty;

    public string IndexNowKeyFileUrl { get; init; } = string.Empty;
}

public sealed class SitemapDocumentResult
{
    public string Content { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/xml";

    public bool WasGeneratedOnDemand { get; init; }
}
