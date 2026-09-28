using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Commands;

public sealed record SaveHistoricalRelationCommand(
    string ParkId,
    Guid? RelationId,
    int? ExpectedRevision,
    HistoricalRelationDraftInput Draft,
    string ActorUserId,
    string? ReviewNote)
    : ICommand<ApplicationResult<HistoricalEditorialMutationResult>>;
