using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Invalide les réponses HTTP publiques qui exposent le snapshot sitemap persistant.
/// </summary>
public interface IPublicSeoResponseCacheInvalidator
{
    Task InvalidateAsync(CancellationToken cancellationToken);
}
