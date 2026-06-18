using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

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
    private readonly ISsrPageCacheInvalidationRequestResolver ssrPageCacheInvalidationRequestResolver;
    private readonly ILogger<InvalidatePublicCachesFilter> logger;

    public InvalidatePublicCachesFilter(
        IOutputCacheStore outputCacheStore,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator,
        ISsrPageCacheInvalidationRequestResolver ssrPageCacheInvalidationRequestResolver,
        ILogger<InvalidatePublicCachesFilter> logger)
    {
        this.outputCacheStore = outputCacheStore;
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator;
        this.ssrPageCacheInvalidationRequestResolver = ssrPageCacheInvalidationRequestResolver;
        this.logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!TryResolveScopes(context, out IReadOnlyCollection<PublicCacheScope> scopes))
        {
            await next();
            return;
        }

        SsrPageCacheInvalidationRequest preExecutionSsrInvalidation = await this.ResolveSsrInvalidationAsync(context, null, scopes);

        ActionExecutedContext executedContext = await next();

        if (!IsSuccessfulResult(executedContext))
        {
            return;
        }

        foreach (string tag in ResolveTags(scopes))
        {
            try
            {
                await this.outputCacheStore.EvictByTagAsync(tag, CancellationToken.None);
            }
            catch (Exception exception)
            {
                this.logger.LogWarning(exception, "Public output cache eviction failed for tag {Tag}.", tag);
            }
        }

        try
        {
            SsrPageCacheInvalidationRequest postExecutionSsrInvalidation = await this.ResolveSsrInvalidationAsync(context, executedContext, scopes);
            SsrPageCacheInvalidationRequest ssrInvalidation = MergeSsrInvalidation(preExecutionSsrInvalidation, postExecutionSsrInvalidation);
            ssrInvalidation = ApplySafetyMode(context, ssrInvalidation);
            if (IsNoOpSsrInvalidation(ssrInvalidation))
            {
                return;
            }

            await this.ssrPageCacheInvalidator.InvalidateAsync(ssrInvalidation, CancellationToken.None);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR page cache invalidation failed.");
        }
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveSsrInvalidationAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        IReadOnlyCollection<PublicCacheScope> scopes)
    {
        try
        {
            return await this.ssrPageCacheInvalidationRequestResolver.ResolveAsync(context, executedContext, scopes, CancellationToken.None);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR page cache invalidation impact resolution failed.");
            return SsrPageCacheInvalidationRequest.AllCaches();
        }
    }

    private static SsrPageCacheInvalidationRequest MergeSsrInvalidation(
        SsrPageCacheInvalidationRequest preExecutionRequest,
        SsrPageCacheInvalidationRequest postExecutionRequest)
    {
        if (preExecutionRequest.All && postExecutionRequest.All)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        if (preExecutionRequest.All)
        {
            return postExecutionRequest;
        }

        if (postExecutionRequest.All)
        {
            return preExecutionRequest;
        }

        return new SsrPageCacheInvalidationRequest
        {
            All = false,
            Paths = preExecutionRequest.Paths
                .Concat(postExecutionRequest.Paths)
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            Prefixes = preExecutionRequest.Prefixes
                .Concat(postExecutionRequest.Prefixes)
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            IncludeSeoDocuments = preExecutionRequest.IncludeSeoDocuments || postExecutionRequest.IncludeSeoDocuments,
            AllowStale = preExecutionRequest.AllowStale && postExecutionRequest.AllowStale,
            Refresh = (preExecutionRequest.Refresh || postExecutionRequest.Refresh)
                && preExecutionRequest.AllowStale
                && postExecutionRequest.AllowStale,
        };
    }

    private static bool IsNoOpSsrInvalidation(SsrPageCacheInvalidationRequest request)
    {
        return !request.All
            && request.Paths.Count == 0
            && request.Prefixes.Count == 0
            && !request.IncludeSeoDocuments;
    }

    private static SsrPageCacheInvalidationRequest ApplySafetyMode(
        ActionExecutingContext context,
        SsrPageCacheInvalidationRequest request)
    {
        if (request.All || !RequiresHardSsrPurge(context))
        {
            return request;
        }

        return new SsrPageCacheInvalidationRequest
        {
            All = request.All,
            Paths = request.Paths,
            Prefixes = request.Prefixes,
            IncludeSeoDocuments = request.IncludeSeoDocuments,
            AllowStale = false,
            Refresh = false,
        };
    }

    private static bool RequiresHardSsrPurge(ActionExecutingContext context)
    {
        if (HttpMethods.IsDelete(context.HttpContext.Request.Method))
        {
            return true;
        }

        return context.ActionArguments.Values.Any(ContainsHardPurgeSignal);
    }

    private static bool ContainsHardPurgeSignal(object? value)
    {
        if (value is null || value is string)
        {
            return false;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                if (ContainsHardPurgeSignal(item))
                {
                    return true;
                }
            }

            return false;
        }

        PropertyInfo? property = value.GetType().GetProperty("IsVisible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property?.GetValue(value) is false)
        {
            return true;
        }

        PropertyInfo? adminReviewStatusProperty = value.GetType().GetProperty("AdminReviewStatus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return string.Equals(adminReviewStatusProperty?.GetValue(value)?.ToString(), "NotRelevant", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveScopes(
        ActionExecutingContext context,
        out IReadOnlyCollection<PublicCacheScope> scopes)
    {
        scopes = Array.Empty<PublicCacheScope>();

        if (!IsMutatingMethod(context.HttpContext.Request.Method))
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
