using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.FactualEvents.Commands;

public sealed record VerifyFactualChangeEventCommand(
    string EventId,
    long ExpectedVersion) : ICommand<ApplicationResult>;
