using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class ReviewLiveTargetMappingCommandHandler
    : ICommandHandler<ReviewLiveTargetMappingCommand, ApplicationResult<LiveTargetMappingResult>>
{
    private readonly ILiveTargetMappingRepository repository;
    private readonly LiveTargetReferenceResolver targetResolver;
    private readonly TimeProvider timeProvider;

    public ReviewLiveTargetMappingCommandHandler(
        ILiveTargetMappingRepository repository,
        LiveTargetReferenceResolver targetResolver,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.targetResolver = targetResolver;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<LiveTargetMappingResult>> HandleAsync(
        ReviewLiveTargetMappingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.MappingId == Guid.Empty
            || command.ExpectedRevision < 1
            || !Enum.IsDefined(command.Decision)
            || string.IsNullOrWhiteSpace(command.ReviewerUserId))
        {
            return ApplicationResult<LiveTargetMappingResult>.Failure(
                LiveDataApplicationErrors.InvalidMapping());
        }

        ExternalLiveTargetMapping? current = await this.repository.GetLatestByIdAsync(
            command.MappingId,
            cancellationToken);
        if (current is null)
        {
            return ApplicationResult<LiveTargetMappingResult>.Failure(
                LiveDataApplicationErrors.MappingNotFound());
        }

        if (current.Revision != command.ExpectedRevision)
        {
            return ApplicationResult<LiveTargetMappingResult>.Failure(
                LiveDataApplicationErrors.Conflict(current.Revision));
        }

        LiveTargetReference? target = null;
        if (command.Decision is LiveTargetMappingDecision.Verify
            or LiveTargetMappingDecision.Correct)
        {
            if (string.IsNullOrWhiteSpace(command.InternalTargetId)
                || string.IsNullOrWhiteSpace(command.ParkId))
            {
                return ApplicationResult<LiveTargetMappingResult>.Failure(
                    LiveDataApplicationErrors.TargetNotFound());
            }

            target = await this.targetResolver.ResolveAsync(
                current.ExternalTarget.Type,
                command.InternalTargetId,
                command.ParkId,
                cancellationToken);
            if (target is null)
            {
                return ApplicationResult<LiveTargetMappingResult>.Failure(
                    LiveDataApplicationErrors.TargetNotFound());
            }

            if (!await this.HasCoherentVerifiedParentAsync(
                current,
                target,
                cancellationToken))
            {
                return ApplicationResult<LiveTargetMappingResult>.Failure(
                    LiveDataApplicationErrors.ParentMappingNotVerified());
            }
        }

        try
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            DateTime reviewedAtUtc = nowUtc < current.RecordedAtUtc
                ? current.RecordedAtUtc
                : nowUtc;
            ExternalLiveTargetMapping revised = command.Decision switch
            {
                LiveTargetMappingDecision.Verify => current.Verify(
                    target!,
                    command.ReviewerUserId,
                    command.ReviewNote,
                    reviewedAtUtc),
                LiveTargetMappingDecision.Correct => current.Correct(
                    target!,
                    command.ReviewerUserId,
                    command.ReviewNote ?? string.Empty,
                    reviewedAtUtc),
                LiveTargetMappingDecision.Suspend => current.Suspend(
                    command.ReviewerUserId,
                    command.ReviewNote ?? string.Empty,
                    reviewedAtUtc),
                LiveTargetMappingDecision.Supersede => current.Supersede(
                    command.ReviewerUserId,
                    command.ReviewNote ?? string.Empty,
                    reviewedAtUtc),
                LiveTargetMappingDecision.Reject => current.Reject(
                    command.ReviewerUserId,
                    command.ReviewNote ?? string.Empty,
                    reviewedAtUtc),
                _ => throw new InvalidOperationException("Unsupported live mapping decision."),
            };
            LiveTargetMappingWriteOutcome outcome = await this.repository.AppendRevisionAsync(
                revised,
                current.Revision,
                cancellationToken);
            if (outcome == LiveTargetMappingWriteOutcome.Created)
            {
                return ApplicationResult<LiveTargetMappingResult>.Success(
                    LiveTargetMappingResultFactory.Create(revised));
            }

            ExternalLiveTargetMapping? latest = await this.repository.GetLatestByIdAsync(
                current.Id,
                cancellationToken);
            return ApplicationResult<LiveTargetMappingResult>.Failure(
                LiveDataApplicationErrors.Conflict(latest?.Revision ?? current.Revision));
        }
        catch (LiveDataValidationException exception)
        {
            bool transitionError = exception.Code == LiveDataErrorCodes.InvalidMappingTransition;
            return ApplicationResult<LiveTargetMappingResult>.Failure(
                transitionError
                    ? LiveDataApplicationErrors.InvalidTransition(exception.Message)
                    : LiveDataApplicationErrors.InvalidMapping(exception.Message));
        }
    }

    private async Task<bool> HasCoherentVerifiedParentAsync(
        ExternalLiveTargetMapping mapping,
        LiveTargetReference target,
        CancellationToken cancellationToken)
    {
        if (mapping.ExternalTarget.Type == LiveTargetType.Park)
        {
            return true;
        }

        ExternalLiveTargetMapping? parent = await this.repository.GetLatestByNaturalKeyAsync(
            mapping.SourceId,
            mapping.ExternalTarget.ParentId!,
            cancellationToken);
        return parent is not null
            && parent.ExternalTarget.Type == LiveTargetType.Park
            && parent.IsEligibleForLiveUse
            && parent.Target is not null
            && string.Equals(parent.Target.ParkId, target.ParkId, StringComparison.Ordinal);
    }
}
