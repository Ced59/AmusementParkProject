using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveQualityIncidentRepository
{
    Task SaveAsync(
        IReadOnlyCollection<LiveQualityIncident> incidents,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LiveQualityIncident>> GetReplayCandidatesAsync(
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
