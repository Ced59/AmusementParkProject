using AmusementPark.Application.Features.FeatureFlags.Models;

namespace AmusementPark.Application.Features.FeatureFlags.Ports;

public interface IFeatureFlagCatalog
{
    IReadOnlyCollection<FeatureFlagDefinition> GetAll();

    FeatureFlagDefinition? Find(string key);
}
