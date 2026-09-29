using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveQualityIncidentRepository
{
    Task<long> CountPendingAsync(
        LiveDataSourceId sourceId,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<long> CountReplayablePendingAsync(
        LiveDataSourceId sourceId,
        IReadOnlyCollection<string> externalTargetIds,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task SaveAsync(
        IReadOnlyCollection<LiveQualityIncident> incidents,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LiveQualityIncident>> GetReplayCandidatesAsync(
        LiveDataSourceId sourceId,
        IReadOnlyCollection<string> externalTargetIds,
        int maximumCount,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<int> MarkResolvedAsync(
        IReadOnlyCollection<Guid> incidentIds,
        DateTime resolvedAtUtc,
        string resolvedByUserId,
        CancellationToken cancellationToken);

    Task MarkReplayAttemptedAsync(
        IReadOnlyCollection<Guid> incidentIds,
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken);
}
