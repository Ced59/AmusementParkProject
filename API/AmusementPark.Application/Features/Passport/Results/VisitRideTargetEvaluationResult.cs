using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record VisitRideTargetEvaluationResult(
    string ParkItemId,
    HistoricalConsistency HistoricalConsistency,
    DateOnly? OpeningDate,
    DateOnly? ClosingDate);
