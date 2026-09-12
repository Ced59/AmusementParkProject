using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class NoOpSeoSitemapRefreshScheduler : ISeoSitemapRefreshScheduler
{
    public Task RequestRefreshAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
