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
