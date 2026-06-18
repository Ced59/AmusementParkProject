using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Common.Measurements;

/// <summary>
/// Convertit et normalise les mesures, avec les valeurs metriques comme source de verite.
/// </summary>
public interface IMeasurementConversionService
{
    double FeetToMeters(double feet);

    double MetersToFeet(double meters);

    double MilesPerHourToKilometersPerHour(double milesPerHour);

    double KilometersPerHourToMilesPerHour(double kilometersPerHour);

    double InchesToCentimeters(double inches);

    double CentimetersToInches(double centimeters);

    void NormalizeAttractionDetails(AttractionDetails details);

    void NormalizeAccessCondition(AttractionAccessCondition condition);
}
