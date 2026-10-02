using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Application.Features.FeatureFlags.Results;
using AmusementPark.Application.Features.LiveData.Ports;

namespace AmusementPark.Application.Features.FeatureFlags.Services;

public sealed class PublicFeatureCapabilityReader
{
    private readonly IFeatureFlagCatalog catalog;
    private readonly IFeatureFlagEvaluator evaluator;
    private readonly ILiveDataSourceCatalog liveDataSourceCatalog;

    public PublicFeatureCapabilityReader(
        IFeatureFlagCatalog catalog,
        IFeatureFlagEvaluator evaluator,
        ILiveDataSourceCatalog liveDataSourceCatalog)
    {
        this.catalog = catalog;
        this.evaluator = evaluator;
        this.liveDataSourceCatalog = liveDataSourceCatalog;
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
                evaluation.IsEnabled && (definition.Key != FeatureFlagKeys.LivePublicExperience
                    || (this.liveDataSourceCatalog.IsPublicReadEnabled
                        && this.liveDataSourceCatalog.PublicPollingTarget is not null))));
        }

        return results.AsReadOnly();
    }
}
