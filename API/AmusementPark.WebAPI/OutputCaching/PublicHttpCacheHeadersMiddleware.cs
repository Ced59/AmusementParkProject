using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Adds browser/proxy HTTP cache headers for anonymous public GET requests.
/// This middleware also runs when OutputCache serves an already cached response.
/// </summary>
public sealed class PublicHttpCacheHeadersMiddleware
{
    private readonly RequestDelegate next;

    public PublicHttpCacheHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (CanRegisterCacheHeaders(context))
        {
            context.Response.OnStarting(static state =>
            {
                HttpContext httpContext = (HttpContext)state;
                PublicHttpCacheHeadersApplicator.Apply(httpContext);
                return Task.CompletedTask;
            }, context);
        }

        await this.next(context);
    }

    private static bool CanRegisterCacheHeaders(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
    }
}
