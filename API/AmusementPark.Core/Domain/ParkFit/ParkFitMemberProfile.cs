using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Faits minimaux et privés nécessaires à une décision individuelle.
/// </summary>
public sealed class ParkFitMemberProfile
{
    public const int MinimumSupportedHeightCentimeters =
        AttractionAccessConditionSemanticEvaluator.MinimumSupportedHeightCentimeters;

    public const int MaximumSupportedHeightCentimeters =
        AttractionAccessConditionSemanticEvaluator.MaximumSupportedHeightCentimeters;

    public ParkFitMemberProfile(
        int? heightCentimeters = null,
        ParkFitAgeRange? ageRange = null,
        bool? canBeAccompanied = null,
        ParkFitAgeRange? availableCompanionAgeRange = null)
    {
        if (heightCentimeters is < MinimumSupportedHeightCentimeters
            or > MaximumSupportedHeightCentimeters)
        {
            throw new ArgumentOutOfRangeException(nameof(heightCentimeters));
        }

        if (availableCompanionAgeRange is not null && canBeAccompanied != true)
        {
            throw new ArgumentException(
                "A companion age range requires explicit accompaniment availability.",
                nameof(availableCompanionAgeRange));
        }

        this.HeightCentimeters = heightCentimeters;
        this.AgeRange = ageRange;
        this.CanBeAccompanied = canBeAccompanied;
        this.AvailableCompanionAgeRange = availableCompanionAgeRange;
    }

    public int? HeightCentimeters { get; }

    public ParkFitAgeRange? AgeRange { get; }

    public bool? CanBeAccompanied { get; }

    public ParkFitAgeRange? AvailableCompanionAgeRange { get; }
}
