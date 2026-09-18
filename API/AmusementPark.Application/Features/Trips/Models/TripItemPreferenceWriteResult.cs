using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripItemPreferenceWriteResult(
    TripChildWriteOutcome Outcome,
    TripItemPreference? Preference = null,
    long? CurrentVersion = null);
