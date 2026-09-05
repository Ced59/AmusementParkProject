using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

public sealed record PassportGlobalStatisticsSource(
    IReadOnlyCollection<int> AvailableYears,
    IReadOnlyCollection<string> AvailableParkIds,
    IReadOnlyCollection<PassportVisitStatisticsObservation> Visits,
    IReadOnlyCollection<PassportRideStatisticsObservation> Rides);
