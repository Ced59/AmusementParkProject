using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.WebAPI.OutputCaching;

public static class PublicLiveCacheLifetimeCalculator
{
    public static TimeSpan DefaultMaximumLifetime => TimeSpan.FromSeconds(30);

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

    public static TimeSpan ResolveLifetime(
        DateTime responseStoredAtUtc,
        DateTime? freshnessTransitionAtUtc)
    {
        return ResolveLifetime(
            responseStoredAtUtc,
            freshnessTransitionAtUtc,
            DefaultMaximumLifetime);
    }

    public static TimeSpan ResolveLifetime(
        DateTime responseStoredAtUtc,
        DateTime? freshnessTransitionAtUtc,
        TimeSpan maximumLifetime)
    {
        if (maximumLifetime <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (!freshnessTransitionAtUtc.HasValue)
        {
            return maximumLifetime;
        }

        TimeSpan untilTransition = freshnessTransitionAtUtc.Value - responseStoredAtUtc;
        if (untilTransition <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        TimeSpan boundedLifetime = untilTransition < maximumLifetime
            ? untilTransition
            : maximumLifetime;
        long wholeSeconds = boundedLifetime.Ticks / TimeSpan.TicksPerSecond;
        return wholeSeconds <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(wholeSeconds);
    }
}
