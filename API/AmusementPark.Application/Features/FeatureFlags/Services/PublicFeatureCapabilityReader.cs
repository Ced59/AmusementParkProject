using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Application.Features.FeatureFlags.Results;

namespace AmusementPark.Application.Features.FeatureFlags.Services;

public sealed class PublicFeatureCapabilityReader
{
    private readonly IFeatureFlagCatalog catalog;
    private readonly IFeatureFlagEvaluator evaluator;

    public PublicFeatureCapabilityReader(
        IFeatureFlagCatalog catalog,
        IFeatureFlagEvaluator evaluator)
    {
        this.catalog = catalog;
        this.evaluator = evaluator;
    }

    public async Task<IReadOnlyCollection<PublicFeatureCapabilityResult>> ReadAsync(
        CancellationToken cancellationToken)
    {
        List<PublicFeatureCapabilityResult> results = new List<PublicFeatureCapabilityResult>();
        foreach (FeatureFlagDefinition definition in this.catalog.GetAll()
            .Where(static definition => definition.ExposeToClient))
        {
            FeatureFlagEvaluation evaluation = await this.evaluator.EvaluateAsync(
                definition.Key,
                cancellationToken);
            results.Add(new PublicFeatureCapabilityResult(
                definition.Key,
                evaluation.IsEnabled));
        }

        return results.AsReadOnly();
    }
}
