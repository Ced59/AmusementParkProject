using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Adds browser/proxy HTTP cache headers for anonymous public GET endpoints.
/// The server-side application cache remains managed separately by OutputCache.
/// </summary>
public sealed class PublicHttpCacheHeadersFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (CanRegisterCacheHeaders(context.HttpContext))
        {
            context.HttpContext.Response.OnStarting(static state =>
            {
                HttpContext httpContext = (HttpContext)state;
                PublicHttpCacheHeadersApplicator.Apply(httpContext);
                return Task.CompletedTask;
            }, context.HttpContext);
        }

        await next();
    }

    private static bool CanRegisterCacheHeaders(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
    }
}
