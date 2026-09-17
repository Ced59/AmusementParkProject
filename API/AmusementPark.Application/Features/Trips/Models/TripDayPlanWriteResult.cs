using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripDayPlanWriteResult(
    TripChildWriteOutcome Outcome,
    TripDayPlan? DayPlan = null,
    long? CurrentVersion = null);
