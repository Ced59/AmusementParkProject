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

    [Fact]
    public void ResolveTransitionAtUtc_WhenForecastEndsFirst_ShouldUseForecastEnd()
    {
        PublicLiveForecastResult forecast = CreateForecast(
            AsOfUtc.AddMinutes(20),
            AsOfUtc.AddMinutes(30));

        DateTime transitionAtUtc =
            PublicLiveCacheLifetimeCalculator.ResolveTransitionAtUtc(forecast);

        Assert.Equal(AsOfUtc.AddMinutes(20), transitionAtUtc);
    }

    [Fact]
    public void ResolveTransitionAtUtc_WhenFreshnessExpiresFirst_ShouldUseFreshnessExpiration()
    {
        PublicLiveForecastResult forecast = CreateForecast(
            AsOfUtc.AddMinutes(30),
            AsOfUtc.AddMinutes(20));

        DateTime transitionAtUtc =
            PublicLiveCacheLifetimeCalculator.ResolveTransitionAtUtc(forecast);

        Assert.Equal(AsOfUtc.AddMinutes(20), transitionAtUtc);
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

    private static PublicLiveForecastResult CreateForecast(
        DateTime forecastToUtc,
        DateTime freshnessExpiresAtUtc)
    {
        return new PublicLiveForecastResult(
            "Attraction",
            "Park",
            "Europe/Paris",
            new LiveWaitForecast(
                forecastToUtc.AddHours(-1),
                forecastToUtc,
                AsOfUtc,
                25d,
                15d,
                35d,
                12),
            LiveWaitForecastBacktestPolicy.StudyVersion,
            LiveWaitForecastBacktestPolicy.CandidateMethod,
            LiveWaitForecastBacktestPolicy.IntervalMethod,
            4d,
            82d,
            AsOfUtc.AddDays(-90),
            AsOfUtc,
            120,
            freshnessExpiresAtUtc,
            new PublicLiveSourceResult(
                "themeparks-wiki",
                "ThemeParks.wiki",
                LiveDataSourceType.AuthorizedAggregator,
                "Powered by ThemeParks.wiki",
                "https://themeparks.wiki/"));
    }
}
