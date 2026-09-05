using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record UserRatingMutationResult(
    bool SourceChanged,
    UserRating Rating,
    RatingAggregate? Aggregate,
    bool WasFencedOut = false);
