using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class PricingDateBoundaryOutputCachePolicy : IOutputCachePolicy
{
    private static readonly TimeSpan MaximumExpiration = TimeSpan.FromMinutes(30);

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ResponseExpirationTimeSpan = ResolveExpiration(DateTimeOffset.UtcNow);
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    internal static TimeSpan ResolveExpiration(DateTimeOffset nowUtc)
    {
        DateTimeOffset nextUtcDay = new DateTimeOffset(
            nowUtc.UtcDateTime.Date.AddDays(1),
            TimeSpan.Zero);
        TimeSpan untilNextUtcDay = nextUtcDay - nowUtc;
        return untilNextUtcDay < MaximumExpiration ? untilNextUtcDay : MaximumExpiration;
    }
}
