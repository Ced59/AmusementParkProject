using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Commands;

public sealed record RetractHistoricalEditorialResourceCommand(
    HistoricalReviewResourceType ResourceType,
    Guid ResourceId,
    int ExpectedRevision,
    string ActorUserId,
    string? ReviewNote)
    : ICommand<ApplicationResult<HistoricalEditorialMutationResult>>;
