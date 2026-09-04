namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// État de cadence propre à une cible. Il ne contient aucune valeur de note.
/// </summary>
public sealed record GlobalRatingSuggestionCadence(
    bool IsEnabled,
    DateTime? LastPresentedAtUtc);
