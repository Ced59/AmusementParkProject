using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

public sealed record PassportGlobalStatisticsSource(
    IReadOnlyCollection<PassportVisitStatisticsObservation> AvailableVisits,
    IReadOnlyCollection<PassportVisitStatisticsObservation> Visits,
    IReadOnlyCollection<PassportRideStatisticsObservation> Rides);
