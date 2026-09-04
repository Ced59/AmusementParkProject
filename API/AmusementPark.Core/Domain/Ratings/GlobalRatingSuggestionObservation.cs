namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Observation privée datée utilisée par la politique de suggestion.
/// </summary>
public sealed record GlobalRatingSuggestionObservation(
    RatingValue Value,
    DateTime RecordedAtUtc);
