using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record GlobalRatingSuggestionPresentedTargetResult(
    RatingTargetType TargetType,
    string TargetId,
    DateTime PresentedAtUtc);
