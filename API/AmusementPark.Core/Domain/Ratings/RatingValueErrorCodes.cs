namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Codes métier stables associés à une valeur de note invalide.
/// </summary>
public static class RatingValueErrorCodes
{
    public const string InvalidValue = "rating.invalid-value";

    public const string InvalidStep = "rating.invalid-step";
}
