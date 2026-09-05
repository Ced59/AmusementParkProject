using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record UserRatingDeletionResult(
    bool SourceChanged,
    RatingAggregate? Aggregate,
    bool WasFencedOut = false);
