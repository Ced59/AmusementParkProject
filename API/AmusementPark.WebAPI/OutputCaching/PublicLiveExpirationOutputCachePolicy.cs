using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class PublicLiveExpirationOutputCachePolicy : IOutputCachePolicy
{
    public const string CacheLifetimeItemKey = "public-live-cache-lifetime";

    public ValueTask CacheRequestAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.HttpContext.Items.TryGetValue(CacheLifetimeItemKey, out object? value)
            || value is not TimeSpan lifetime)
        {
            return ValueTask.CompletedTask;
        }

        if (lifetime <= TimeSpan.Zero)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        context.ResponseExpirationTimeSpan = lifetime;
        return ValueTask.CompletedTask;
    }
}
