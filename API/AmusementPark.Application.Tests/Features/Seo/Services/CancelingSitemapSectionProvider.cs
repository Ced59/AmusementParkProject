using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

internal sealed class CancelingSitemapSectionProvider : ISitemapSectionProvider
{
    public string Key => SitemapSectionKeys.Parks;

    public string FileName => "parks.xml";

    public string DisplayName => "Parcs";

    public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        throw new OperationCanceledException();
    }
}
