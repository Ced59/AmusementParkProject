using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

public interface IPublicSeoContextProvider
{
    Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken);
}

public interface IPublicSeoUpdateNotifier
{
    Task NotifyAsync(PublicSeoUpdate update, CancellationToken cancellationToken);
}

public interface ISeoSitemapRefreshScheduler
{
    Task RequestRefreshAsync(CancellationToken cancellationToken);
}
