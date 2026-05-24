using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.Parks.Services;

/// <summary>
/// Calcule une distance métier entre deux coordonnées géographiques.
/// </summary>
public interface IParkDistanceCalculator
{
    /// <summary>
    /// Calcule la distance orthodromique en kilomètres entre deux points.
    /// </summary>
    double CalculateKilometers(GeoPoint source, GeoPoint target);

    /// <summary>
    /// Calcule une durée indicative de trajet, en minutes, à partir d'une distance en kilomètres.
    /// </summary>
    int EstimateTravelDurationMinutes(double distanceKilometers);
}
