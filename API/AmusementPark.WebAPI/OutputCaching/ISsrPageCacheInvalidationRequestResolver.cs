using AmusementPark.Application.Ports;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmusementPark.WebAPI.OutputCaching;

public interface ISsrPageCacheInvalidationRequestResolver
{
    Task<SsrPageCacheInvalidationRequest> ResolveAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        IReadOnlyCollection<PublicCacheScope> scopes,
        CancellationToken cancellationToken);
}
