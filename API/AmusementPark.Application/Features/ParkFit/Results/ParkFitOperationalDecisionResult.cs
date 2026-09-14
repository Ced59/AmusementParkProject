using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Results;

public sealed record ParkFitOperationalDecisionResult(
    ParkFitOperationalDecisionType Type,
    string Reason,
    DateTime DecidedAtUtc,
    long Revision);
