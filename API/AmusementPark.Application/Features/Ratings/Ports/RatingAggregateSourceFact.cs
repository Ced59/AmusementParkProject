using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record RatingAggregateSourceFact(
    RatingTargetType TargetType,
    string TargetId,
    long UniqueContributorCount,
    long RatingObservationCount,
    double RatingSum);
