using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveOperationalScopeResultFactory
{
    public LiveOperationalScopeResult Create(
        LiveOperationalControlScope scope,
        string displayName,
        string? parentDisplayName,
        LiveOperationalControl? control,
        LiveOperationalGateSnapshot gate)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(gate);
        return new LiveOperationalScopeResult(
            scope.Type,
            scope.SourceId.Value,
            scope.ExternalEntityId,
            scope.InternalParkId,
            scope.TargetType,
            scope.InternalTargetId,
            displayName,
            parentDisplayName,
            control?.CollectionEnabled ?? true,
            control?.PublicReadEnabled ?? true,
            gate.AllowsCollection(
                scope.InternalParkId,
                scope.TargetType,
                scope.InternalTargetId),
            gate.AllowsPublicRead(
                scope.InternalParkId,
                scope.TargetType,
                scope.InternalTargetId),
            control?.Revision ?? 0,
            control?.Reason,
            control?.ChangedByUserId,
            control?.RecordedAtUtc);
    }
}
