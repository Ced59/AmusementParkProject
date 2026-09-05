namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record ParkRankingContributorFacts(
    string ParkId,
    long UniqueContributorCount,
    long RatingObservationCount,
    long DirectParkContributorCount,
    long ItemContributorCount);
