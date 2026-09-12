using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareSelectionResult(
    IReadOnlyCollection<PassportProfileShareYearCandidateResult> Years,
    IReadOnlyCollection<PassportProfileShareParkCandidateResult> Parks,
    IReadOnlyCollection<PassportProfileShareRatingCandidateResult> Ratings,
    int MaximumSelectedYears,
    int MaximumSelectedParks,
    IReadOnlyCollection<int>? SavedSelectedYears,
    IReadOnlyCollection<string>? SavedSelectedParkIds,
    IReadOnlyCollection<string>? SavedSelectedRatingKeys,
    string? SavedPublicCaption,
    ShareVisibility SavedVisibility,
    bool SavedAllowsComparisons,
    bool HasSavedSnapshot);
