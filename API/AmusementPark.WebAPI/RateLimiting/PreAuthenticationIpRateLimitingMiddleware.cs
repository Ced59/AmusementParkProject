using System.Threading.RateLimiting;
using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AmusementPark.WebAPI.RateLimiting;

public sealed class PreAuthenticationIpRateLimitingMiddleware
    : IMiddleware,
      IDisposable,
      IAsyncDisposable
{
    private readonly PartitionedRateLimiter<HttpContext> limiter;

    public PreAuthenticationIpRateLimitingMiddleware(IConfiguration configuration)
    {
        this.limiter = RateLimitingServiceCollectionExtensions
            .CreatePreAuthenticationIpLimiter(configuration);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        using RateLimitLease lease = await this.limiter.AcquireAsync(
            context,
            permitCount: 1,
            context.RequestAborted);
        if (!lease.IsAcquired)
        {
            await RateLimitingServiceCollectionExtensions.WriteRateLimitRejectionAsync(
                context,
                lease,
                context.RequestAborted);
            return;
        }

        await next(context);
    }

    public ValueTask DisposeAsync()
    {
        return this.limiter.DisposeAsync();
    }

    public void Dispose()
    {
        this.limiter.Dispose();
    }
}
