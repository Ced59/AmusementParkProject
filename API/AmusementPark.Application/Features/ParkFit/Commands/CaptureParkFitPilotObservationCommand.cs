using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Commands;

public sealed record CaptureParkFitPilotObservationCommand(
    ParkFitPilotEventKind EventKind,
    ParkFitPilotResultBand? ResultBand,
    ParkFitPilotUnknownLevel? UnknownLevel,
    ParkFitPilotDurationBand? DurationBand,
    ParkFitPilotFailureKind? FailureKind,
    ParkFitPilotComparisonSize? ComparisonSize,
    string? MethodVersion,
    IReadOnlyCollection<ParkFitDataQualityIssue> QualityIssues) : ICommand<ApplicationResult>;
