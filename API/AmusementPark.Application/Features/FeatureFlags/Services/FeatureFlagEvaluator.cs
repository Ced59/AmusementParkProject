using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.FeatureFlags.Services;

public sealed class FeatureFlagEvaluator : IFeatureFlagEvaluator
{
    private readonly IFeatureFlagCatalog catalog;
    private readonly IFeatureFlagStateRepository repository;
    private readonly ILogger<FeatureFlagEvaluator> logger;

    public FeatureFlagEvaluator(
        IFeatureFlagCatalog catalog,
        IFeatureFlagStateRepository repository,
        ILogger<FeatureFlagEvaluator> logger)
    {
        this.catalog = catalog;
        this.repository = repository;
        this.logger = logger;
    }

    public async Task<FeatureFlagEvaluation> EvaluateAsync(
        string key,
        CancellationToken cancellationToken)
    {
        FeatureFlagDefinition? definition = this.catalog.Find(key);
        if (definition is null)
        {
            return new FeatureFlagEvaluation(
                key,
                false,
                FeatureFlagEvaluationSource.SafeFallback,
                0);
        }

        try
        {
            return await this.EvaluateCoreAsync(
                definition,
                new HashSet<string>(StringComparer.Ordinal),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            this.logger.LogError(
                exception,
                "Feature flag {FeatureFlagKey} evaluation failed; using its safe fallback.",
                definition.Key);
            return new FeatureFlagEvaluation(
                definition.Key,
                definition.SafeFallbackEnabled,
                FeatureFlagEvaluationSource.SafeFallback,
                0);
        }
    }

    private async Task<FeatureFlagEvaluation> EvaluateCoreAsync(
        FeatureFlagDefinition definition,
        HashSet<string> evaluatedKeys,
        CancellationToken cancellationToken)
    {
        if (!evaluatedKeys.Add(definition.Key))
        {
            return new FeatureFlagEvaluation(
                definition.Key,
                definition.SafeFallbackEnabled,
                FeatureFlagEvaluationSource.SafeFallback,
                0);
        }

        if (!definition.Environments.Contains("*", StringComparer.OrdinalIgnoreCase)
            && !definition.Environments.Contains(
                this.repository.Environment,
                StringComparer.OrdinalIgnoreCase))
        {
            return new FeatureFlagEvaluation(
                definition.Key,
                definition.SafeFallbackEnabled,
                FeatureFlagEvaluationSource.SafeFallback,
                0);
        }

        foreach (string dependencyKey in definition.Dependencies)
        {
            FeatureFlagDefinition dependency = this.catalog.Find(dependencyKey)!;
            FeatureFlagEvaluation dependencyEvaluation = await this.EvaluateCoreAsync(
                dependency,
                evaluatedKeys,
                cancellationToken);
            if (!dependencyEvaluation.IsEnabled)
            {
                return new FeatureFlagEvaluation(
                    definition.Key,
                    false,
                    FeatureFlagEvaluationSource.Dependency,
                    0);
            }
        }

        FeatureFlagState? state = await this.repository.GetLatestAsync(
            definition.Key,
            cancellationToken);
        return state?.EnabledOverride is bool enabledOverride
            ? new FeatureFlagEvaluation(
                definition.Key,
                enabledOverride,
                FeatureFlagEvaluationSource.Override,
                state.Revision)
            : new FeatureFlagEvaluation(
                definition.Key,
                definition.DefaultEnabled,
                FeatureFlagEvaluationSource.Default,
                state?.Revision ?? 0);
    }
}
