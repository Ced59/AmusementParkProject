using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.Parks.Services;

/// <summary>
/// Implémentation Haversine centralisée pour éviter les calculs de distance dispersés dans les composants.
/// </summary>
public sealed class ParkDistanceCalculator : IParkDistanceCalculator
{
    private const double EarthRadiusKilometers = 6371.0088d;
    private const double EstimatedRoadSpeedKilometersPerHour = 70d;

    public double CalculateKilometers(GeoPoint source, GeoPoint target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        double sourceLatitudeRadians = ToRadians(source.Latitude);
        double targetLatitudeRadians = ToRadians(target.Latitude);
        double latitudeDeltaRadians = ToRadians(target.Latitude - source.Latitude);
        double longitudeDeltaRadians = ToRadians(target.Longitude - source.Longitude);

        double haversine = Math.Pow(Math.Sin(latitudeDeltaRadians / 2d), 2d)
            + Math.Cos(sourceLatitudeRadians) * Math.Cos(targetLatitudeRadians) * Math.Pow(Math.Sin(longitudeDeltaRadians / 2d), 2d);

        double angularDistance = 2d * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1d - haversine));
        return EarthRadiusKilometers * angularDistance;
    }

    public int EstimateTravelDurationMinutes(double distanceKilometers)
    {
        if (!double.IsFinite(distanceKilometers) || distanceKilometers <= 0d)
        {
            return 0;
        }

        double durationHours = distanceKilometers / EstimatedRoadSpeedKilometersPerHour;
        return Math.Max(1, Convert.ToInt32(Math.Ceiling(durationHours * 60d)));
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}
