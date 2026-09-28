using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportHistoricalTarget(
    string ParkItemId,
    string ParkId,
    string Name,
    string Category,
    HistoricalOperationalState OperationalState,
    HistoricalConsistency HistoricalConsistency,
    HistoricalTargetReference HistoricalTarget,
    bool IsHistoricalOnly,
    string? MainImageId,
    string? ZoneId,
    string? LifecycleStatus,
    DateOnly? OpeningDate,
    DateOnly? ClosingDate,
    bool IsValidationFallback = false,
    string? HistoricalClassification = null,
    bool HasCanonicalEvidence = false,
    bool HasCanonicalNameEvidence = false,
    bool HasCanonicalClassificationEvidence = false);
