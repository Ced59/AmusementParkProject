using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Commands;

public sealed record SaveHistoricalFactCommand(
    string ParkId,
    Guid? FactId,
    int? ExpectedRevision,
    HistoricalFactDraftInput Draft,
    string ActorUserId,
    string? ReviewNote)
    : ICommand<ApplicationResult<HistoricalEditorialMutationResult>>;
