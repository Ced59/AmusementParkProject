using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

public sealed record PassportYearStatisticsSource(
    IReadOnlyCollection<PassportVisitStatisticsObservation> Visits,
    IReadOnlyCollection<PassportRideStatisticsObservation> Rides);
