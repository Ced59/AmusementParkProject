using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class UpdateLiveOperationalControlCommandHandler
    : ICommandHandler<
        UpdateLiveOperationalControlCommand,
        ApplicationResult<LiveOperationalScopeResult>>
{
    private readonly ILiveOperationalControlRepository repository;
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly LiveTargetReferenceResolver targetResolver;
    private readonly ILiveOperationalGate operationalGate;
    private readonly LiveOperationalScopeResultFactory resultFactory;
    private readonly LiveOperationalWriteCoordinator coordinator;
    private readonly TimeProvider timeProvider;

    internal UpdateLiveOperationalControlCommandHandler(
        ILiveOperationalControlRepository repository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveDataSourceCatalog sourceCatalog,
        LiveTargetReferenceResolver targetResolver,
        ILiveOperationalGate operationalGate,
        LiveOperationalScopeResultFactory resultFactory,
        TimeProvider? timeProvider = null)
        : this(
            repository,
            mappingRepository,
            sourceCatalog,
            targetResolver,
            operationalGate,
            resultFactory,
            new LiveOperationalWriteCoordinator(),
            timeProvider)
    {
    }

    public UpdateLiveOperationalControlCommandHandler(
        ILiveOperationalControlRepository repository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveDataSourceCatalog sourceCatalog,
        LiveTargetReferenceResolver targetResolver,
        ILiveOperationalGate operationalGate,
        LiveOperationalScopeResultFactory resultFactory,
        LiveOperationalWriteCoordinator coordinator,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.mappingRepository = mappingRepository;
        this.sourceCatalog = sourceCatalog;
        this.targetResolver = targetResolver;
        this.operationalGate = operationalGate;
        this.resultFactory = resultFactory;
        this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<LiveOperationalScopeResult>> HandleAsync(
        UpdateLiveOperationalControlCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ExpectedRevision < 0
            || !Enum.IsDefined(command.ScopeType)
            || command.TargetType.HasValue && !Enum.IsDefined(command.TargetType.Value))
        {
            return Invalid();
        }

        try
        {
            LiveDataSourceId sourceId = LiveDataSourceId.Parse(command.SourceId);
            LivePollingTarget? configuredTarget = this.sourceCatalog.ConfiguredPollingTarget;
            if (configuredTarget is null || configuredTarget.SourceId != sourceId)
            {
                return Invalid();
            }

            LiveOperationalControlScope scope = new LiveOperationalControlScope(
                command.ScopeType,
                sourceId,
                command.ExternalEntityId,
                command.InternalParkId,
                command.TargetType,
                command.InternalTargetId);
            if (scope.Type != LiveOperationalScopeType.Source
                && !string.Equals(
                    scope.ExternalEntityId,
                    configuredTarget.ExternalEntityId,
                    StringComparison.Ordinal))
            {
                return Invalid();
            }

            (string DisplayName, string? ParentDisplayName)? labels =
                await this.ResolveAndValidateLabelsAsync(
                    configuredTarget,
                    scope,
                    cancellationToken);
            if (labels is null)
            {
                return Invalid();
            }

            return await this.coordinator.RunAsync(
                sourceId,
                configuredTarget.ExternalEntityId,
                async _ =>
                {
                    CancellationToken commitCancellationToken = CancellationToken.None;
                    LiveOperationalControl? current = await this.repository.GetLatestAsync(
                        scope,
                        commitCancellationToken);
                    if ((current is null && command.ExpectedRevision != 0)
                        || (current is not null && current.Revision != command.ExpectedRevision))
                    {
                        return ApplicationResult<LiveOperationalScopeResult>.Failure(
                            LiveDataApplicationErrors.OperationalControlConflict(
                                current?.Revision ?? 0));
                    }

                    LiveOperationalGateSnapshot gate = await this.operationalGate.LoadAsync(
                        sourceId,
                        configuredTarget.ExternalEntityId,
                        commitCancellationToken);

                    DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
                    LiveOperationalControl next = current is null
                        ? new LiveOperationalControl(
                            Guid.NewGuid(),
                            scope,
                            command.CollectionEnabled,
                            command.PublicReadEnabled,
                            1,
                            null,
                            command.ChangedByUserId,
                            command.Reason,
                            nowUtc)
                        : current.Revise(
                            command.CollectionEnabled,
                            command.PublicReadEnabled,
                            command.ChangedByUserId,
                            command.Reason,
                            nowUtc);
                    LiveOperationalControlWriteOutcome outcome =
                        await this.repository.AppendRevisionAsync(
                            next,
                            command.ExpectedRevision,
                            commitCancellationToken);
                    if (outcome != LiveOperationalControlWriteOutcome.Created)
                    {
                        LiveOperationalControl? latest = await this.repository.GetLatestAsync(
                            scope,
                            commitCancellationToken);
                        return ApplicationResult<LiveOperationalScopeResult>.Failure(
                            LiveDataApplicationErrors.OperationalControlConflict(
                                latest?.Revision ?? 0));
                    }

                    LiveOperationalGateSnapshot committedGate = gate.WithControl(next);
                    return ApplicationResult<LiveOperationalScopeResult>.Success(
                        this.resultFactory.Create(
                            scope,
                            labels.Value.DisplayName,
                            labels.Value.ParentDisplayName,
                            next,
                            committedGate));
                },
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is LiveDataValidationException
                or ArgumentException)
        {
            return Invalid(exception.Message);
        }
    }

    private async Task<(string DisplayName, string? ParentDisplayName)?> ResolveAndValidateLabelsAsync(
        LivePollingTarget configuredTarget,
        LiveOperationalControlScope scope,
        CancellationToken cancellationToken)
    {
        LiveDataSourcePresentation? source = this.sourceCatalog.Find(scope.SourceId);
        if (source is null)
        {
            return null;
        }

        if (scope.Type == LiveOperationalScopeType.Source)
        {
            return (source.Source.DisplayName, null);
        }

        LiveTargetType targetType = scope.Type == LiveOperationalScopeType.Park
            ? LiveTargetType.Park
            : scope.TargetType!.Value;
        string targetId = scope.Type == LiveOperationalScopeType.Park
            ? scope.InternalParkId!
            : scope.InternalTargetId!;
        LiveTargetReference? target = await this.targetResolver.ResolveAsync(
            targetType,
            targetId,
            scope.InternalParkId!,
            cancellationToken);
        if (target is null)
        {
            return null;
        }

        IReadOnlyCollection<LivePublicTargetCoverage> coverage =
            await this.mappingRepository.GetEligiblePublicTargetCoverageByParkAsync(
                scope.SourceId,
                configuredTarget.ExternalEntityId,
                scope.InternalParkId!,
                cancellationToken);
        bool isCovered = scope.Type == LiveOperationalScopeType.Park
            ? coverage.Count > 0
            : coverage.Any(item => string.Equals(
                item.InternalTargetId,
                targetId,
                StringComparison.Ordinal));
        if (!isCovered)
        {
            return null;
        }

        return (target.DisplayName, target.Type == LiveTargetType.Park
            ? null
            : target.ParkDisplayName);
    }

    private static ApplicationResult<LiveOperationalScopeResult> Invalid(string? message = null)
    {
        return ApplicationResult<LiveOperationalScopeResult>.Failure(
            LiveDataApplicationErrors.InvalidOperationalControl(message));
    }
}
