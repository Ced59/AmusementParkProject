using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.WebAPI.OutputCaching;

public static class PublicLiveCacheLifetimeCalculator
{
    private static readonly TimeSpan MaximumLifetime = TimeSpan.FromSeconds(30);

    public static TimeSpan Resolve(PublicLiveTargetResult target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Resolve(target.AsOfUtc, new[] { target });
    }

    public static TimeSpan Resolve(PublicParkLiveItemsResult parkItems)
    {
        ArgumentNullException.ThrowIfNull(parkItems);
        return Resolve(parkItems.AsOfUtc, parkItems.Items);
    }

    private static TimeSpan Resolve(
        DateTime asOfUtc,
        IEnumerable<PublicLiveTargetResult> targets)
    {
        DateTime? earliestTransitionUtc = targets
            .Where(static target => target.Availability == PublicLiveAvailability.Current)
            .Select(static target => target.FreshnessTransitionAtUtc)
            .Where(static transition => transition.HasValue)
            .Min();
        if (!earliestTransitionUtc.HasValue)
        {
            return MaximumLifetime;
        }

        TimeSpan untilTransition = earliestTransitionUtc.Value - asOfUtc;
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
