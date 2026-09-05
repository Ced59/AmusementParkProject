namespace AmusementPark.Application.Features.Passport.Results;

public sealed record GlobalRatingSuggestionsResult(
    bool IsAvailable,
    bool IsEnabled,
    int MinimumNewObservationCount,
    int CooldownDays,
    IReadOnlyCollection<GlobalRatingSuggestionResult> Suggestions);
