using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.FactualEvents.Commands;

public sealed record CorrectFactualChangeEventCommand(
    string EventId,
    long ExpectedVersion,
    string SupersedingEventId) : ICommand<ApplicationResult>;
