using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Commands;

public sealed record UpdateLiveOperationalControlCommand(
    LiveOperationalScopeType ScopeType,
    string SourceId,
    string? ExternalEntityId,
    string? InternalParkId,
    LiveTargetType? TargetType,
    string? InternalTargetId,
    bool CollectionEnabled,
    bool PublicReadEnabled,
    int ExpectedRevision,
    string Reason,
    string ChangedByUserId)
    : ICommand<ApplicationResult<LiveOperationalScopeResult>>;
