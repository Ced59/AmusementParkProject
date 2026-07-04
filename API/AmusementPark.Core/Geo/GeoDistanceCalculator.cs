namespace AmusementPark.Core.Geo;

/// <summary>
/// Calculs geographiques metier partages.
/// </summary>
public static class GeoDistanceCalculator
{
    private const double EarthRadiusKilometers = 6371.0088d;
    private const double EstimatedRoadSpeedKilometersPerHour = 70d;

    public static double CalculateKilometers(GeoPoint source, GeoPoint target)
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

    public static int EstimateTravelDurationMinutes(double distanceKilometers)
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
