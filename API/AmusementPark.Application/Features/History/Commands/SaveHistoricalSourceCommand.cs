using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Commands;

public sealed record SaveHistoricalSourceCommand(
    Guid? SourceId,
    int? ExpectedRevision,
    HistoricalSourceDraftInput Draft,
    string ActorUserId,
    string? ReviewNote)
    : ICommand<ApplicationResult<HistoricalEditorialMutationResult>>;
