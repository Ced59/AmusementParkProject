using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.OutputCaching;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class PublicLiveCacheLifetimeCalculatorTests
{
    private static readonly DateTime AsOfUtc =
        new DateTime(2026, 9, 29, 12, 0, 29, DateTimeKind.Utc);

    [Fact]
    public void ResolveLifetime_WhenFreshnessChangesBeforeMaximum_ShouldStopAtTransition()
    {
        PublicLiveTargetResult target = CreateTarget(AsOfUtc.AddSeconds(1.9));
        DateTime? transitionAtUtc = PublicLiveCacheLifetimeCalculator.ResolveTransitionAtUtc(target);

        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.ResolveLifetime(AsOfUtc, transitionAtUtc);

        Assert.Equal(TimeSpan.FromSeconds(1), lifetime);
    }

    [Fact]
    public void ResolveLifetime_WhenResponseWorkConsumesRemainingSecond_ShouldDisableCaching()
    {
        PublicLiveTargetResult target = CreateTarget(AsOfUtc.AddSeconds(1.1));
        DateTime? transitionAtUtc = PublicLiveCacheLifetimeCalculator.ResolveTransitionAtUtc(target);

        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.ResolveLifetime(
            AsOfUtc.AddMilliseconds(200),
            transitionAtUtc);

        Assert.Equal(TimeSpan.Zero, lifetime);
    }

    [Fact]
    public void ResolveLifetime_WhenNoCurrentObservation_ShouldUseMaximumLifetime()
    {
        PublicLiveTargetResult target = CreateTarget(null) with
        {
            Availability = PublicLiveAvailability.Expired,
        };

        DateTime? transitionAtUtc = PublicLiveCacheLifetimeCalculator.ResolveTransitionAtUtc(target);
        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.ResolveLifetime(AsOfUtc, transitionAtUtc);

        Assert.Equal(TimeSpan.FromSeconds(30), lifetime);
    }

    private static PublicLiveTargetResult CreateTarget(DateTime? refreshAfterUtc)
    {
        return new PublicLiveTargetResult(
            "item-1",
            LiveTargetType.ParkItem,
            "Attraction",
            "park-1",
            "Park",
            PublicLiveAvailability.Current,
            LiveOperationalStatus.Open,
            Array.Empty<PublicLiveQueueResult>(),
            AsOfUtc,
            AsOfUtc.AddMinutes(-1),
            AsOfUtc.AddMinutes(-1),
            60,
            LiveFreshnessState.Fresh,
            AsOfUtc.AddMinutes(14),
            refreshAfterUtc,
            null,
            LiveDataConfidence.Medium);
    }
}
