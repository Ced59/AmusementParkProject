using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Mesure la part des types d'attractions demandés présents dans un parc.
/// </summary>
public sealed class ParkFitPreferenceCoverageSubscoreEvaluator
{
    public ParkFitSubscore Evaluate(
        IReadOnlyCollection<ParkItemType> preferredAttractionTypes,
        IReadOnlyCollection<ParkItem> attractions)
    {
        ArgumentNullException.ThrowIfNull(preferredAttractionTypes);
        ArgumentNullException.ThrowIfNull(attractions);

        List<ParkItemType> preferences = preferredAttractionTypes
            .Distinct()
            .ToList();
        if (preferences.Any(static type => !IsPreciseAttractionType(type)))
        {
            throw new ArgumentException(
                "Preferences must contain precise attraction types only.",
                nameof(preferredAttractionTypes));
        }

        List<ParkItem> snapshot = attractions.ToList();
        if (snapshot.Any(static attraction => attraction is null))
        {
            throw new ArgumentException(
                "Attractions cannot contain null entries.",
                nameof(attractions));
        }

        if (preferences.Count == 0)
        {
            return BuildKnown(100m, 100m, hasUnknownFacts: false);
        }

        if (snapshot.Count == 0)
        {
            return BuildUnknown();
        }

        List<ParkItem> knownAttractions = snapshot
            .Where(static attraction => attraction.Category == ParkItemCategory.Attraction
                && IsPreciseAttractionType(attraction.Type))
            .ToList();
        if (knownAttractions.Count == 0)
        {
            return BuildUnknown();
        }

        HashSet<ParkItemType> availableTypes = knownAttractions
            .Select(static attraction => attraction.Type)
            .ToHashSet();
        int matchedPreferenceCount = preferences.Count(availableTypes.Contains);
        decimal value = 100m * matchedPreferenceCount / preferences.Count;
        decimal coveragePercent = 100m * knownAttractions.Count / snapshot.Count;

        return BuildKnown(
            value,
            coveragePercent,
            knownAttractions.Count != snapshot.Count);
    }

    private static bool IsPreciseAttractionType(ParkItemType type)
    {
        return Enum.IsDefined(type)
            && type is not ParkItemType.Attraction and not ParkItemType.Other
            && ParkItemAdministrationDefaults.IsTypeAllowedForCategory(
                ParkItemCategory.Attraction,
                type);
    }

    private static ParkFitSubscore BuildKnown(
        decimal value,
        decimal coveragePercent,
        bool hasUnknownFacts)
    {
        List<ParkFitSubscoreReasonCode> reasons = new List<ParkFitSubscoreReasonCode>
        {
            ParkFitSubscoreReasonCode.KnownFactsNormalized,
        };
        if (hasUnknownFacts)
        {
            reasons.Add(ParkFitSubscoreReasonCode.UnknownFactsExcluded);
        }

        return new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
            ParkFitSubscoreState.Known,
            Round(value),
            Round(coveragePercent),
            ParkFitDataConfidence.High,
            reasons);
    }

    private static ParkFitSubscore BuildUnknown()
    {
        return new ParkFitSubscore(
            ParkFitSubscoreKind.PreferenceCoverage,
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
