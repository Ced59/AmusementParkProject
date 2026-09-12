using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

public sealed record PassportParkStatisticsSource(
    IReadOnlyCollection<PassportVisitStatisticsObservation> Visits,
    IReadOnlyCollection<PassportRideStatisticsObservation> Rides,
    RatingValue? CurrentGlobalRating,
    IReadOnlyCollection<PassportCurrentItemRatingObservation> CurrentItemRatings);
