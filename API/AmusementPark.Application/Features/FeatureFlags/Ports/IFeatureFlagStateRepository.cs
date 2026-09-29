using AmusementPark.Application.Features.FeatureFlags.Models;

namespace AmusementPark.Application.Features.FeatureFlags.Ports;

public interface IFeatureFlagStateRepository
{
    string Environment { get; }

    Task<FeatureFlagState?> GetLatestAsync(string key, CancellationToken cancellationToken);

    Task<FeatureFlagWriteOutcome> AppendRevisionAsync(
        FeatureFlagState state,
        int expectedRevision,
        CancellationToken cancellationToken);
}
