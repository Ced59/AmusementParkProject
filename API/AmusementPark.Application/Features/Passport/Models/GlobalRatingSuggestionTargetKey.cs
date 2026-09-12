using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record GlobalRatingSuggestionTargetKey(
    RatingTargetType TargetType,
    string TargetId);
