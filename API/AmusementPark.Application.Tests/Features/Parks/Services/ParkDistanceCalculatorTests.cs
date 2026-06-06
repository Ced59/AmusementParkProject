using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Core.Geo;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Services;

public sealed class ParkDistanceCalculatorTests
{
    [Fact]
    public void CalculateKilometers_WhenSamePointProvided_ShouldReturnZero()
    {
        ParkDistanceCalculator calculator = new ParkDistanceCalculator();
        GeoPoint point = new GeoPoint(50.6372d, 3.0633d);

        double result = calculator.CalculateKilometers(point, point);

        Assert.Equal(0d, result, precision: 8);
    }

    [Fact]
    public void CalculateKilometers_WhenParisAndLilleProvided_ShouldReturnApproximateDistance()
    {
        ParkDistanceCalculator calculator = new ParkDistanceCalculator();
        GeoPoint paris = new GeoPoint(48.8566d, 2.3522d);
        GeoPoint lille = new GeoPoint(50.6292d, 3.0573d);

        double result = calculator.CalculateKilometers(paris, lille);

        Assert.InRange(result, 200d, 215d);
    }

    [Fact]
    public void CalculateKilometers_WhenSourceIsNull_ShouldThrow()
    {
        ParkDistanceCalculator calculator = new ParkDistanceCalculator();

        Assert.Throws<ArgumentNullException>(() => calculator.CalculateKilometers(null!, new GeoPoint(0d, 0d)));
    }

    [Fact]
    public void CalculateKilometers_WhenTargetIsNull_ShouldThrow()
    {
        ParkDistanceCalculator calculator = new ParkDistanceCalculator();

        Assert.Throws<ArgumentNullException>(() => calculator.CalculateKilometers(new GeoPoint(0d, 0d), null!));
    }

    [Theory]
    [InlineData(0d, 0)]
    [InlineData(-1d, 0)]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(1d, 1)]
    [InlineData(70d, 60)]
    [InlineData(71d, 61)]
    public void EstimateTravelDurationMinutes_WhenDistanceProvided_ShouldReturnExpectedRoundedDuration(double distanceKilometers, int expected)
    {
        ParkDistanceCalculator calculator = new ParkDistanceCalculator();

        int result = calculator.EstimateTravelDurationMinutes(distanceKilometers);

        Assert.Equal(expected, result);
    }
}
