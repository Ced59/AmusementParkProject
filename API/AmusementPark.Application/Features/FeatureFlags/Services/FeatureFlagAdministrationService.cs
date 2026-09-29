using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Application.Features.FeatureFlags.Results;

namespace AmusementPark.Application.Features.FeatureFlags.Services;

public sealed class FeatureFlagAdministrationService
{
    private readonly IFeatureFlagCatalog catalog;
    private readonly IFeatureFlagStateRepository repository;
    private readonly IFeatureFlagEvaluator evaluator;
    private readonly TimeProvider timeProvider;

    public FeatureFlagAdministrationService(
        IFeatureFlagCatalog catalog,
        IFeatureFlagStateRepository repository,
        IFeatureFlagEvaluator evaluator,
        TimeProvider? timeProvider = null)
    {
        this.catalog = catalog;
        this.repository = repository;
        this.evaluator = evaluator;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<FeatureFlagAdministrationResult>>> ListAsync(
        CancellationToken cancellationToken)
    {
        List<FeatureFlagAdministrationResult> results = new List<FeatureFlagAdministrationResult>();
        foreach (FeatureFlagDefinition definition in this.catalog.GetAll())
        {
            FeatureFlagState? state;
            try
            {
                state = await this.repository.GetLatestAsync(definition.Key, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return ApplicationResult<IReadOnlyCollection<FeatureFlagAdministrationResult>>.Failure(
                    FeatureFlagApplicationErrors.StorageUnavailable());
            }

            FeatureFlagEvaluation evaluation = await this.evaluator.EvaluateAsync(
                definition.Key,
                cancellationToken);
            results.Add(ToResult(definition, this.repository.Environment, state, evaluation));
        }

        return ApplicationResult<IReadOnlyCollection<FeatureFlagAdministrationResult>>.Success(
            results.AsReadOnly());
    }

    public async Task<ApplicationResult<FeatureFlagAdministrationResult>> UpdateAsync(
        string key,
        bool? enabledOverride,
        int expectedRevision,
        string reason,
        string changedByUserId,
        CancellationToken cancellationToken)
    {
        FeatureFlagDefinition? definition = this.catalog.Find(key);
        if (definition is null)
        {
            return ApplicationResult<FeatureFlagAdministrationResult>.Failure(
                FeatureFlagApplicationErrors.UnknownKey());
        }

        string normalizedReason = reason?.Trim() ?? string.Empty;
        if (normalizedReason.Length < 10 || normalizedReason.Length > 500)
        {
            return ApplicationResult<FeatureFlagAdministrationResult>.Failure(
                FeatureFlagApplicationErrors.InvalidReason());
        }

        try
        {
            FeatureFlagState? current = await this.repository.GetLatestAsync(key, cancellationToken);
            int currentRevision = current?.Revision ?? 0;
            if (expectedRevision < 0 || currentRevision != expectedRevision)
            {
                return ApplicationResult<FeatureFlagAdministrationResult>.Failure(
                    FeatureFlagApplicationErrors.Conflict(currentRevision));
            }

            FeatureFlagState next = new FeatureFlagState(
                current?.Id ?? Guid.NewGuid(),
                definition.Key,
                this.repository.Environment,
                enabledOverride,
                currentRevision + 1,
                currentRevision == 0 ? null : currentRevision,
                changedByUserId,
                normalizedReason,
                this.timeProvider.GetUtcNow().UtcDateTime);
            FeatureFlagWriteOutcome outcome = await this.repository.AppendRevisionAsync(
                next,
                expectedRevision,
                cancellationToken);
            if (outcome != FeatureFlagWriteOutcome.Created)
            {
                FeatureFlagState? latest = await this.repository.GetLatestAsync(key, cancellationToken);
                return ApplicationResult<FeatureFlagAdministrationResult>.Failure(
                    FeatureFlagApplicationErrors.Conflict(latest?.Revision ?? currentRevision));
            }

            FeatureFlagEvaluation evaluation = await this.evaluator.EvaluateAsync(
                definition.Key,
                cancellationToken);
            return ApplicationResult<FeatureFlagAdministrationResult>.Success(
                ToResult(definition, this.repository.Environment, next, evaluation));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ApplicationResult<FeatureFlagAdministrationResult>.Failure(
                FeatureFlagApplicationErrors.StorageUnavailable());
        }
    }

    private static FeatureFlagAdministrationResult ToResult(
        FeatureFlagDefinition definition,
        string environment,
        FeatureFlagState? state,
        FeatureFlagEvaluation evaluation)
    {
        return new FeatureFlagAdministrationResult(
            definition,
            environment,
            evaluation.IsEnabled,
            evaluation.Source,
            state?.EnabledOverride,
            state?.Revision ?? 0,
            state?.Reason,
            state?.ChangedByUserId,
            state?.RecordedAtUtc);
    }
}
