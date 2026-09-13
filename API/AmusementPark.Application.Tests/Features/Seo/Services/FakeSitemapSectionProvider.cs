using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

internal sealed class FakeSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IReadOnlyCollection<SitemapUrlEntry> urls;

    public FakeSitemapSectionProvider(string key, string fileName, string displayName, IReadOnlyCollection<SitemapUrlEntry> urls)
    {
        this.Key = key;
        this.FileName = fileName;
        this.DisplayName = displayName;
        this.urls = urls;
    }

    public string Key { get; }

    public string FileName { get; }

    public string DisplayName { get; }

    public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.urls);
    }
}
