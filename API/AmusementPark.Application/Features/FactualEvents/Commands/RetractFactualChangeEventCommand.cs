using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.FactualEvents.Commands;

public sealed record RetractFactualChangeEventCommand(
    string EventId,
    long ExpectedVersion,
    string ReasonCode) : ICommand<ApplicationResult>;
