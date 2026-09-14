using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Commands;

public sealed record ChangeParkFitOperationalStatusCommand(
    string ParkId,
    ParkFitRecommendationState TargetState,
    string ActorUserId,
    string Reason,
    long ExpectedRevision) : ICommand<ApplicationResult>;
