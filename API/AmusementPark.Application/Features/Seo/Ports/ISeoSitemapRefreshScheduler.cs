using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

public interface ISeoSitemapRefreshScheduler
{
    Task RequestRefreshAsync(CancellationToken cancellationToken);
}
