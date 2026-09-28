using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Commands;

public sealed record CreateLiveTargetMappingCandidateCommand(
    string SourceId,
    LiveTargetType TargetType,
    string ExternalTargetId,
    string? ExternalParentTargetId,
    string ExternalDisplayName,
    string? ExternalParentDisplayName,
    string ExternalCountryCode,
    string? SuggestedInternalTargetId,
    string? SuggestedParkId)
    : ICommand<ApplicationResult<LiveTargetMappingResult>>;
