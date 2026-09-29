using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Application.Features.LiveData.Ports;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LivePublicExperienceGate : ILivePublicExperienceGate
{
    private readonly IFeatureFlagEvaluator evaluator;

    public LivePublicExperienceGate(IFeatureFlagEvaluator evaluator)
    {
        this.evaluator = evaluator;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
    {
        FeatureFlagEvaluation evaluation = await this.evaluator.EvaluateAsync(
            FeatureFlagKeys.LivePublicExperience,
            cancellationToken);
        return evaluation.IsEnabled;
    }
}
