using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveOperationalObservationWriter
{
    private readonly ILiveLatestObservationRepository repository;
    private readonly ILiveOperationalGate operationalGate;
    private readonly LiveOperationalWriteCoordinator coordinator;
    private readonly LiveHistoryCaptureService? historyCaptureService;

    public LiveOperationalObservationWriter(
        ILiveLatestObservationRepository repository,
        ILiveOperationalGate operationalGate,
        LiveOperationalWriteCoordinator coordinator,
        LiveHistoryCaptureService? historyCaptureService = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.operationalGate = operationalGate
            ?? throw new ArgumentNullException(nameof(operationalGate));
        this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        this.historyCaptureService = historyCaptureService;
    }

    public Task<LiveOperationalObservationWriteResult> WriteAsync(
        LiveDataSourceId sourceId,
        string externalEntityId,
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observations);
        return this.coordinator.RunAsync(
            sourceId,
            externalEntityId,
            async boundaryCancellationToken =>
            {
                LiveOperationalGateSnapshot gate = await this.operationalGate.LoadAsync(
                    sourceId,
                    externalEntityId,
                    boundaryCancellationToken);
                LiveLatestObservation[] accepted = observations
                    .Where(observation => gate.AllowsCollection(
                        observation.Target.ParkId,
                        observation.Target.Type,
                        observation.Target.Id))
                    .ToArray();
                LiveLatestObservationWriteResult result = await this.repository.WriteLatestAsync(
                    accepted,
                    boundaryCancellationToken);
                if (this.historyCaptureService is not null
                    && result.CommittedObservations.Count > 0)
                {
                    await this.historyCaptureService.CaptureAsync(
                        result.CommittedObservations,
                        boundaryCancellationToken);
                }

                return new LiveOperationalObservationWriteResult(result, accepted);
            },
            cancellationToken);
    }
}
