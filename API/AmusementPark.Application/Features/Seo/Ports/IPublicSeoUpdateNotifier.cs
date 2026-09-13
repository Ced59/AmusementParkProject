using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

public interface IPublicSeoUpdateNotifier
{
    Task NotifyAsync(PublicSeoUpdate update, CancellationToken cancellationToken);
}
