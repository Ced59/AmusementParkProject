using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class PublicLiveExpirationOutputCachePolicy : IOutputCachePolicy
{
    public const string FreshnessTransitionItemKey = "public-live-freshness-transition";

    private readonly TimeProvider timeProvider;

    public PublicLiveExpirationOutputCachePolicy(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

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
        if (!context.HttpContext.Items.TryGetValue(FreshnessTransitionItemKey, out object? value))
        {
            return ValueTask.CompletedTask;
        }

        DateTime? freshnessTransitionAtUtc = value is DateTime transitionAtUtc
            ? transitionAtUtc
            : null;
        DateTime responseStoredAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.ResolveLifetime(
            responseStoredAtUtc,
            freshnessTransitionAtUtc);

        if (lifetime <= TimeSpan.Zero)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        context.ResponseExpirationTimeSpan = lifetime;
        return ValueTask.CompletedTask;
    }
}
