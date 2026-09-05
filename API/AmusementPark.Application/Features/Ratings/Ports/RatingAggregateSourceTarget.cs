using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record RatingAggregateSourceTarget(
    RatingTargetType TargetType,
    string TargetId);
