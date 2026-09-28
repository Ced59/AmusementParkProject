using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.Application.Features.LiveData.Commands;

public sealed record ReviewLiveTargetMappingCommand(
    Guid MappingId,
    int ExpectedRevision,
    LiveTargetMappingDecision Decision,
    string? InternalTargetId,
    string? ParkId,
    string ReviewerUserId,
    string? ReviewNote)
    : ICommand<ApplicationResult<LiveTargetMappingResult>>;
