using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record GlobalRatingSuggestionSource(
    RatingTargetType TargetType,
    string TargetId,
    string ParkId,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    RatingValue CurrentGlobalRating,
    DateTime CurrentGlobalRatingUpdatedAtUtc,
    IReadOnlyCollection<GlobalRatingSuggestionObservation> Observations);

public sealed record GlobalRatingSuggestionTargetKey(
    RatingTargetType TargetType,
    string TargetId);

public sealed record GlobalRatingSuggestionTargetState(
    RatingTargetType TargetType,
    string TargetId,
    DateTime? LastPresentedAtUtc,
    DateTime? LastAcceptedAtUtc,
    DateTime? LastDismissedAtUtc,
    bool IsAwaitingResolution);

public enum GlobalRatingSuggestionInteractionType
{
    Presented = 1,
    Accepted = 2,
    Dismissed = 3,
}
