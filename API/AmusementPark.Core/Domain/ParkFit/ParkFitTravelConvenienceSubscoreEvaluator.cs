using AmusementPark.Core.Geo;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Transforme une distance directe en préférence souple, sans inventer un trajet.
/// </summary>
public sealed class ParkFitTravelConvenienceSubscoreEvaluator
{
    public const decimal MaximumScoredDistanceKilometers = 1000m;

    public ParkFitTravelEvaluation Evaluate(
        GeoPoint? origin,
        GeoPoint? destination,
        DateTime evaluatedAtUtc)
    {
        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The travel evaluation timestamp must use UTC.",
                nameof(evaluatedAtUtc));
        }

        if (origin is null)
        {
            return new ParkFitTravelEvaluation(
                new ParkFitSubscore(
                    ParkFitSubscoreKind.TravelConvenience,
                    ParkFitSubscoreState.NotApplicable,
                    null,
                    0m,
                    ParkFitDataConfidence.Unknown),
                null);
        }

        if (destination is null)
        {
            return new ParkFitTravelEvaluation(
                new ParkFitSubscore(
                    ParkFitSubscoreKind.TravelConvenience,
                    ParkFitSubscoreState.Unknown,
                    null,
                    0m,
                    ParkFitDataConfidence.Unknown,
                    new[] { ParkFitSubscoreReasonCode.NoKnownFact }),
                null);
        }

        double distanceKilometers = GeoDistanceCalculator.CalculateKilometers(
            origin,
            destination);
        ParkFitTravelDistance distance = new ParkFitTravelDistance(
            distanceKilometers,
            ParkFitDistanceMethod.DirectGeodesic,
            evaluatedAtUtc);
        decimal boundedDistance = Math.Min(
            Convert.ToDecimal(distance.DistanceKilometers),
            MaximumScoredDistanceKilometers);
        decimal value = Math.Round(
            100m * (MaximumScoredDistanceKilometers - boundedDistance)
                / MaximumScoredDistanceKilometers,
            2,
            MidpointRounding.AwayFromZero);

        return new ParkFitTravelEvaluation(
            new ParkFitSubscore(
                ParkFitSubscoreKind.TravelConvenience,
                ParkFitSubscoreState.Known,
                value,
                100m,
                ParkFitDataConfidence.Medium,
                new[] { ParkFitSubscoreReasonCode.DirectDistanceCalculated }),
            distance);
    }
}
