using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripItemPreferenceSummaryResult(
    string ParkId,
    string ParkName,
    string ParkItemId,
    string ParkItemName,
    string? MainImageId,
    int MustDoCount,
    int WantToDoCount,
    int OptionalCount,
    int NotForMeCount,
    int UnansweredCount,
    TripPreferenceCompatibility Compatibility,
    bool IsCompatibilityKnown,
    bool HasIndividualConstraint,
    bool IsGroupPriority,
    string? OfficialStatus,
    string? OfficialSourceUrl,
    DateTime? OfficialStatusVerifiedAtUtc,
    TripItemDecisionResult? Decision);
