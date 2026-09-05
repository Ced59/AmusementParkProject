namespace AmusementPark.Application.Features.Passport.Results;

public sealed record GlobalRatingSuggestionPresentationResult(
    bool IsAvailable,
    bool IsEnabled,
    IReadOnlyCollection<GlobalRatingSuggestionPresentedTargetResult> PresentedTargets);
