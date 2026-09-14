using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Mesure la part connue de l'offre d'attractions située en intérieur.
/// </summary>
public sealed class ParkFitIndoorResilienceSubscoreEvaluator
{
    public ParkFitSubscore Evaluate(
        bool indoorPreferenceEnabled,
        IReadOnlyCollection<ParkItem> attractions)
    {
        ArgumentNullException.ThrowIfNull(attractions);

        if (!indoorPreferenceEnabled)
        {
            return new ParkFitSubscore(
                ParkFitSubscoreKind.IndoorResilience,
                ParkFitSubscoreState.NotApplicable,
                null,
                0m,
                ParkFitDataConfidence.Unknown);
        }

        List<ParkItem> snapshot = attractions.ToList();
        if (snapshot.Any(static attraction => attraction is null))
        {
            throw new ArgumentException(
                "Attractions cannot contain null entries.",
                nameof(attractions));
        }

        if (snapshot.Any(static attraction => attraction.Category != ParkItemCategory.Attraction))
        {
            throw new ArgumentException(
                "Attractions must use the attraction category.",
                nameof(attractions));
        }

        if (snapshot.Count == 0)
        {
            return BuildUnknown();
        }

        List<bool> knownClassifications = snapshot
            .Where(static attraction => attraction.AttractionDetails?.IsIndoor.HasValue == true)
            .Select(static attraction => attraction.AttractionDetails!.IsIndoor!.Value)
            .ToList();
        if (knownClassifications.Count == 0)
        {
            return BuildUnknown();
        }

        decimal value = 100m * knownClassifications.Count(static isIndoor => isIndoor)
            / knownClassifications.Count;
        decimal coveragePercent = 100m * knownClassifications.Count / snapshot.Count;
        List<ParkFitSubscoreReasonCode> reasons = new List<ParkFitSubscoreReasonCode>
        {
            ParkFitSubscoreReasonCode.KnownFactsNormalized,
        };
        if (knownClassifications.Count != snapshot.Count)
        {
            reasons.Add(ParkFitSubscoreReasonCode.UnknownFactsExcluded);
        }

        return new ParkFitSubscore(
            ParkFitSubscoreKind.IndoorResilience,
            ParkFitSubscoreState.Known,
            Round(value),
            Round(coveragePercent),
            ParkFitDataConfidence.High,
            reasons);
    }

    private static ParkFitSubscore BuildUnknown()
    {
        return new ParkFitSubscore(
            ParkFitSubscoreKind.IndoorResilience,
            ParkFitSubscoreState.Unknown,
            null,
            0m,
            ParkFitDataConfidence.Unknown,
            new[] { ParkFitSubscoreReasonCode.NoKnownFact });
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
