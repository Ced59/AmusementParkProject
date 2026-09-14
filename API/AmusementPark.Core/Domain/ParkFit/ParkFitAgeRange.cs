namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Tranche d'âge déclarative qui évite de collecter une date de naissance.
/// </summary>
public sealed class ParkFitAgeRange
{
    public const int MaximumSupportedAgeYears = 130;

    public ParkFitAgeRange(int minimumYears, int maximumYears)
    {
        if (minimumYears < 0 || minimumYears > MaximumSupportedAgeYears)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumYears));
        }

        if (maximumYears < minimumYears || maximumYears > MaximumSupportedAgeYears)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumYears));
        }

        this.MinimumYears = minimumYears;
        this.MaximumYears = maximumYears;
    }

    public int MinimumYears { get; }

    public int MaximumYears { get; }
}
