using System.Globalization;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class PublicLiveExpirationOutputCachePolicy : IOutputCachePolicy
{
    public const string FreshnessTransitionItemKey = "public-live-freshness-transition";
    public const string MaximumLifetimeItemKey = "public-live-maximum-lifetime";
    public const string GenerationItemKey = "public-live-cache-generation";

    private readonly TimeProvider timeProvider;
    private readonly PublicLiveCacheGeneration generation;

    public PublicLiveExpirationOutputCachePolicy(
        TimeProvider timeProvider,
        PublicLiveCacheGeneration generation)
    {
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.generation = generation ?? throw new ArgumentNullException(nameof(generation));
    }

    public ValueTask CacheRequestAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        long requestGeneration = this.generation.Current;
        context.HttpContext.Items[GenerationItemKey] = requestGeneration;
        context.CacheVaryByRules.VaryByValues[GenerationItemKey] =
            requestGeneration.ToString(CultureInfo.InvariantCulture);
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
        if (!context.HttpContext.Items.TryGetValue(GenerationItemKey, out object? generationValue)
            || generationValue is not long requestGeneration
            || requestGeneration != this.generation.Current)
        {
            context.AllowCacheStorage = false;
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return ValueTask.CompletedTask;
        }

        context.HttpContext.Items.TryGetValue(FreshnessTransitionItemKey, out object? value);
        DateTime? freshnessTransitionAtUtc = value is DateTime transitionAtUtc
            ? transitionAtUtc
            : null;
        TimeSpan maximumLifetime = context.HttpContext.Items.TryGetValue(
                MaximumLifetimeItemKey,
                out object? lifetimeValue)
            && lifetimeValue is TimeSpan configuredLifetime
                ? configuredLifetime
                : PublicLiveCacheLifetimeCalculator.DefaultMaximumLifetime;
        DateTime responseStoredAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.ResolveLifetime(
            responseStoredAtUtc,
            freshnessTransitionAtUtc,
            maximumLifetime);

        if (lifetime <= TimeSpan.Zero)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        context.ResponseExpirationTimeSpan = lifetime;
        return ValueTask.CompletedTask;
    }
}
