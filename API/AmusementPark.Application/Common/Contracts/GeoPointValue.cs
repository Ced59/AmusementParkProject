namespace AmusementPark.Application.Common.Contracts;

/// <summary>
/// Coordonnées géographiques transport-agnostiques.
/// </summary>
/// <param name="Latitude">Latitude décimale.</param>
/// <param name="Longitude">Longitude décimale.</param>
public sealed record GeoPointValue(double Latitude, double Longitude);
