using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Après une écriture réussie sur un endpoint annoté
/// <see cref="InvalidatesPublicCacheAttribute"/>, invalide les caches publics
/// concernés : l'OutputCache serveur (par tag) puis le cache de pages SSR. Cela
/// garantit que les modifications administrateur sont visibles immédiatement
/// côté public, sans attendre l'expiration naturelle des caches.
/// </summary>
public sealed class InvalidatePublicCachesFilter : IAsyncActionFilter
{
    private readonly IOutputCacheStore outputCacheStore;
    private readonly ISsrPageCacheInvalidator ssrPageCacheInvalidator;

    public InvalidatePublicCachesFilter(
        IOutputCacheStore outputCacheStore,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator)
    {
        this.outputCacheStore = outputCacheStore;
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ActionExecutedContext executedContext = await next();

        if (!TryResolveScopes(context, executedContext, out IReadOnlyCollection<PublicCacheScope> scopes))
        {
            return;
        }

        CancellationToken cancellationToken = context.HttpContext.RequestAborted;

        foreach (string tag in ResolveTags(scopes))
        {
            await this.outputCacheStore.EvictByTagAsync(tag, cancellationToken);
        }

        await this.ssrPageCacheInvalidator.InvalidateAllAsync(cancellationToken);
    }

    private static bool TryResolveScopes(
        ActionExecutingContext context,
        ActionExecutedContext executedContext,
        out IReadOnlyCollection<PublicCacheScope> scopes)
    {
        scopes = Array.Empty<PublicCacheScope>();

        if (!IsMutatingMethod(context.HttpContext.Request.Method))
        {
            return false;
        }

        if (!IsSuccessfulResult(executedContext))
        {
            return false;
        }

        InvalidatesPublicCacheAttribute? attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<InvalidatesPublicCacheAttribute>()
            .FirstOrDefault();

        if (attribute is null || attribute.Scopes.Count == 0)
        {
            return false;
        }

        scopes = attribute.Scopes;
        return true;
    }

    private static bool IsMutatingMethod(string method)
    {
        return HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method)
            || HttpMethods.IsDelete(method);
    }

    private static bool IsSuccessfulResult(ActionExecutedContext executedContext)
    {
        if (executedContext.Exception is not null && !executedContext.ExceptionHandled)
        {
            return false;
        }

        int statusCode = ResolveStatusCode(executedContext);
        return statusCode is >= StatusCodes.Status200OK and < StatusCodes.Status400BadRequest;
    }

    private static int ResolveStatusCode(ActionExecutedContext executedContext)
    {
        if (executedContext.Result is IStatusCodeActionResult statusCodeResult && statusCodeResult.StatusCode.HasValue)
        {
            return statusCodeResult.StatusCode.Value;
        }

        return executedContext.HttpContext.Response.StatusCode;
    }

    private static IReadOnlyCollection<string> ResolveTags(IReadOnlyCollection<PublicCacheScope> scopes)
    {
        return scopes
            .Select(static scope => scope switch
            {
                PublicCacheScope.Data => ApiOutputCachePolicyNames.PublicDataTag,
                PublicCacheScope.ReferenceData => ApiOutputCachePolicyNames.PublicReferenceDataTag,
                PublicCacheScope.Seo => ApiOutputCachePolicyNames.PublicSeoTag,
                _ => throw new ArgumentOutOfRangeException(nameof(scopes), scope, "Unsupported public cache scope.")
            })
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
