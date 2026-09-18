using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripProgramEvidenceProjection(
    IReadOnlyCollection<TripProgramDayEvidenceResult> Days,
    IReadOnlyCollection<TripProgramDayFact> DayFacts,
    IReadOnlyCollection<TripProgramTravelSegmentResult> TravelSegments);
