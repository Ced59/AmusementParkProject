using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.Application.Features.Seo.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Handlers;

internal sealed class FakeSitemapSectionProvider : ISitemapSectionProvider
{
    public string Key => SitemapSectionKeys.Static;

    public string FileName => "static.xml";

    public string DisplayName => "Pages statiques";

    public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<SitemapUrlEntry> urls = new[]
        {
            new SitemapUrlEntry("/fr/home", new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc), "daily", 1.0m),
        };

        return Task.FromResult(urls);
    }
}
