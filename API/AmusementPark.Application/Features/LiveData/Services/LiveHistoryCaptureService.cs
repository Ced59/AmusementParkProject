using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveHistoryCaptureService
{
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly ILiveHistoryRepository repository;

    public LiveHistoryCaptureService(
        ILiveDataSourceCatalog sourceCatalog,
        ILiveHistoryRepository repository)
    {
        this.sourceCatalog = sourceCatalog ?? throw new ArgumentNullException(nameof(sourceCatalog));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<int> CaptureAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observations);
        int capturedCount = 0;
        foreach (IGrouping<LiveDataSourceId, LiveLatestObservation> sourceGroup in observations
                     .GroupBy(static observation => observation.Provenance.SourceId))
        {
            LiveDataSourcePresentation? presentation = this.sourceCatalog.Find(sourceGroup.Key);
            LiveDataSource? source = presentation?.Source;
            LiveHistoryRetentionPolicy? retentionPolicy = source?.HistoryRetentionPolicy;
            if (source is null
                || !source.CanPoll
                || !source.UsagePolicy.HistoricalStorageAllowed
                || retentionPolicy is null)
            {
                continue;
            }

            LiveLatestObservation[] eligible = sourceGroup
                .Where(observation => string.Equals(
                    observation.Provenance.UsagePolicyVersion,
                    source.UsagePolicy.Version,
                    StringComparison.Ordinal))
                .ToArray();
            if (eligible.Length == 0)
            {
                continue;
            }

            await this.repository.StoreAsync(eligible, retentionPolicy, cancellationToken);
            capturedCount += eligible.Length;
        }

        return capturedCount;
    }
}
