using AmusementPark.Application.Features.FeatureFlags.Models;

namespace AmusementPark.Application.Features.FeatureFlags.Ports;

public interface IFeatureFlagEvaluator
{
    Task<FeatureFlagEvaluation> EvaluateAsync(string key, CancellationToken cancellationToken);
}
