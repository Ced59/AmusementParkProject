using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record GlobalRatingSuggestionResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    double CurrentGlobalRating,
    double LatestObservationRating,
    double RecentAverage,
    double HistoricalMedian,
    int NewObservationCount,
    int RecentObservationCount,
    GlobalRatingSuggestionReason Reason,
    DateTime LatestObservationAtUtc);

public sealed record GlobalRatingSuggestionsResult(
    bool IsAvailable,
    bool IsEnabled,
    int MinimumNewObservationCount,
    int CooldownDays,
    IReadOnlyCollection<GlobalRatingSuggestionResult> Suggestions);

public sealed record GlobalRatingSuggestionPreferenceResult(
    bool IsAvailable,
    bool IsEnabled);

public sealed record GlobalRatingSuggestionPresentationResult(
    bool IsAvailable,
    bool IsEnabled,
    IReadOnlyCollection<GlobalRatingSuggestionPresentedTargetResult> PresentedTargets);

public sealed record GlobalRatingSuggestionPresentedTargetResult(
    RatingTargetType TargetType,
    string TargetId,
    DateTime PresentedAtUtc);
