using AmusementPark.Application.Features.ParkWeather.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkWeather.Services;

public sealed class ParkWeatherHistoricalComparisonDateResolverTests
{
    [Fact]
    public void ResolveComparisonDate_WhenForecastDateIsLeapDayAndTargetYearIsNotLeap_ShouldUseFebruaryTwentyEighth()
    {
        ParkWeatherHistoricalComparisonDateResolver resolver = new ParkWeatherHistoricalComparisonDateResolver();

        DateOnly result = resolver.ResolveComparisonDate(new DateOnly(2028, 2, 29), 1);

        Assert.Equal(new DateOnly(2027, 2, 28), result);
    }

    [Fact]
    public void ResolveComparisonDate_WhenForecastDateIsLeapDayAndTargetYearIsLeap_ShouldKeepFebruaryTwentyNinth()
    {
        ParkWeatherHistoricalComparisonDateResolver resolver = new ParkWeatherHistoricalComparisonDateResolver();

        DateOnly result = resolver.ResolveComparisonDate(new DateOnly(2028, 2, 29), 4);

        Assert.Equal(new DateOnly(2024, 2, 29), result);
    }
}
