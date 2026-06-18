using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Common.Measurements;

/// <summary>
/// Service pur de conversion entre le systeme metrique canonique et le systeme imperial.
/// </summary>
public sealed class MeasurementConversionService : IMeasurementConversionService
{
    public static MeasurementConversionService Instance { get; } = new MeasurementConversionService();

    private const double MetersPerFoot = 0.3048d;
    private const double KilometersPerMile = 1.609344d;
    private const double CentimetersPerInch = 2.54d;

    public double FeetToMeters(double feet)
    {
        return RoundMetric(feet * MetersPerFoot);
    }

    public double MetersToFeet(double meters)
    {
        return RoundImperial(meters / MetersPerFoot);
    }

    public double MilesPerHourToKilometersPerHour(double milesPerHour)
    {
        return RoundMetric(milesPerHour * KilometersPerMile);
    }

    public double KilometersPerHourToMilesPerHour(double kilometersPerHour)
    {
        return RoundImperial(kilometersPerHour / KilometersPerMile);
    }

    public double InchesToCentimeters(double inches)
    {
        return RoundMetric(inches * CentimetersPerInch);
    }

    public double CentimetersToInches(double centimeters)
    {
        return RoundImperial(centimeters / CentimetersPerInch);
    }

    public void NormalizeAttractionDetails(AttractionDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        details.HeightInMeters = ResolveMetricFromMetricOrImperial(details.HeightInMeters, details.HeightInFeet, FeetToMeters);
        details.HeightInFeet = details.HeightInMeters.HasValue ? MetersToFeet(details.HeightInMeters.Value) : null;

        details.LengthInMeters = ResolveMetricFromMetricOrImperial(details.LengthInMeters, details.LengthInFeet, FeetToMeters);
        details.LengthInFeet = details.LengthInMeters.HasValue ? MetersToFeet(details.LengthInMeters.Value) : null;

        details.SpeedInKmH = ResolveMetricFromMetricOrImperial(details.SpeedInKmH, details.SpeedInMph, MilesPerHourToKilometersPerHour);
        details.SpeedInMph = details.SpeedInKmH.HasValue ? KilometersPerHourToMilesPerHour(details.SpeedInKmH.Value) : null;

        details.DropInMeters = ResolveMetricFromMetricOrImperial(details.DropInMeters, details.DropInFeet, FeetToMeters);
        details.DropInFeet = details.DropInMeters.HasValue ? MetersToFeet(details.DropInMeters.Value) : null;

        foreach (AttractionAccessCondition condition in details.AccessConditions)
        {
            NormalizeAccessCondition(condition);
        }
    }

    public void NormalizeAccessCondition(AttractionAccessCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (condition.Unit == AttractionAccessConditionUnit.Inch && condition.Value.HasValue)
        {
            condition.Value = InchesToCentimeters(condition.Value.Value);
            condition.Unit = AttractionAccessConditionUnit.Centimeter;
        }
    }

    private static double? ResolveMetricFromMetricOrImperial(double? metricValue, double? imperialValue, Func<double, double> toMetric)
    {
        if (metricValue.HasValue)
        {
            return RoundMetric(metricValue.Value);
        }

        return imperialValue.HasValue ? toMetric(imperialValue.Value) : null;
    }

    private static double RoundMetric(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static double RoundImperial(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
