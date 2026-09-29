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
    public void Resolve_WhenFreshnessChangesBeforeMaximum_ShouldStopAtTransition()
    {
        PublicLiveTargetResult target = CreateTarget(AsOfUtc.AddSeconds(1.9));

        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.Resolve(target);

        Assert.Equal(TimeSpan.FromSeconds(1), lifetime);
    }

    [Fact]
    public void Resolve_WhenFreshnessTransitionIsImmediate_ShouldDisableCaching()
    {
        PublicLiveTargetResult target = CreateTarget(AsOfUtc.AddMilliseconds(500));

        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.Resolve(target);

        Assert.Equal(TimeSpan.Zero, lifetime);
    }

    [Fact]
    public void Resolve_WhenNoCurrentObservation_ShouldUseMaximumLifetime()
    {
        PublicLiveTargetResult target = CreateTarget(null) with
        {
            Availability = PublicLiveAvailability.Expired,
        };

        TimeSpan lifetime = PublicLiveCacheLifetimeCalculator.Resolve(target);

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
