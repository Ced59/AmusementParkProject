using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

public interface IPublicSeoContextProvider
{
    Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken);
}
