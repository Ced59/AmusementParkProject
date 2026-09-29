using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.WebAPI.OutputCaching;

public static class PublicLiveCacheLifetimeCalculator
{
    private static readonly TimeSpan MaximumLifetime = TimeSpan.FromSeconds(30);

    public static DateTime? ResolveTransitionAtUtc(PublicLiveTargetResult target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return target.Availability == PublicLiveAvailability.Current
            ? target.FreshnessTransitionAtUtc
            : null;
    }

    public static DateTime? ResolveTransitionAtUtc(PublicParkLiveItemsResult parkItems)
    {
        ArgumentNullException.ThrowIfNull(parkItems);
        return parkItems.Items
            .Where(static target => target.Availability == PublicLiveAvailability.Current)
            .Select(static target => target.FreshnessTransitionAtUtc)
            .Where(static transition => transition.HasValue)
            .Min();
    }

    public static DateTime ResolveTransitionAtUtc(PublicLiveForecastResult forecast)
    {
        ArgumentNullException.ThrowIfNull(forecast);
        return forecast.Forecast.ForecastToUtc < forecast.FreshnessExpiresAtUtc
            ? forecast.Forecast.ForecastToUtc
            : forecast.FreshnessExpiresAtUtc;
    }

    public static TimeSpan ResolveLifetime(
        DateTime responseStoredAtUtc,
        DateTime? freshnessTransitionAtUtc)
    {
        if (!freshnessTransitionAtUtc.HasValue)
        {
            return MaximumLifetime;
        }

        TimeSpan untilTransition = freshnessTransitionAtUtc.Value - responseStoredAtUtc;
        if (untilTransition <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        TimeSpan boundedLifetime = untilTransition < MaximumLifetime
            ? untilTransition
            : MaximumLifetime;
        long wholeSeconds = boundedLifetime.Ticks / TimeSpan.TicksPerSecond;
        return wholeSeconds <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(wholeSeconds);
    }
}
