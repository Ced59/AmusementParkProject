using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveOperationalGate : ILiveOperationalGate
{
    private readonly ILiveOperationalControlRepository repository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly LiveOperationalControlPolicy policy;

    public LiveOperationalGate(
        ILiveOperationalControlRepository repository,
        ILiveDataSourceCatalog sourceCatalog,
        LiveOperationalControlPolicy policy)
    {
        this.repository = repository;
        this.sourceCatalog = sourceCatalog;
        this.policy = policy;
    }

    public async Task<LiveOperationalGateSnapshot> LoadAsync(
        LiveDataSourceId sourceId,
        string externalEntityId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<LiveOperationalControl> controls =
            await this.repository.GetLatestBySourceAsync(sourceId, cancellationToken);
        LivePollingTarget? configuredTarget = this.sourceCatalog.ConfiguredPollingTarget;
        bool matchesConfiguredPilot = configuredTarget is not null
            && configuredTarget.SourceId == sourceId
            && string.Equals(
                configuredTarget.ExternalEntityId,
                externalEntityId,
                StringComparison.Ordinal);
        return new LiveOperationalGateSnapshot(
            matchesConfiguredPilot && this.sourceCatalog.IsCollectionEnabled,
            matchesConfiguredPilot && this.sourceCatalog.IsPublicReadEnabled,
            externalEntityId,
            controls,
            this.policy);
    }
}
