using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class CreateLiveTargetMappingCandidateCommandHandler
    : ICommandHandler<
        CreateLiveTargetMappingCandidateCommand,
        ApplicationResult<LiveTargetMappingResult>>
{
    private readonly ILiveTargetMappingRepository repository;
    private readonly LiveTargetReferenceResolver targetResolver;
    private readonly TimeProvider timeProvider;

    public CreateLiveTargetMappingCandidateCommandHandler(
        ILiveTargetMappingRepository repository,
        LiveTargetReferenceResolver targetResolver,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.targetResolver = targetResolver;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<LiveTargetMappingResult>> HandleAsync(
        CreateLiveTargetMappingCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.TargetType)
            || string.IsNullOrWhiteSpace(command.SourceId)
            || string.IsNullOrWhiteSpace(command.ExternalTargetId)
            || string.IsNullOrWhiteSpace(command.ExternalDisplayName)
            || string.IsNullOrWhiteSpace(command.ExternalCountryCode)
            || string.IsNullOrWhiteSpace(command.SuggestedInternalTargetId)
                != string.IsNullOrWhiteSpace(command.SuggestedParkId))
        {
            return ApplicationResult<LiveTargetMappingResult>.Failure(
                LiveDataApplicationErrors.InvalidMapping());
        }

        try
        {
            LiveDataSourceId sourceId = LiveDataSourceId.Parse(command.SourceId);
            ExternalLiveTargetMapping? existing = await this.repository
                .GetLatestByNaturalKeyAsync(
                    sourceId,
                    command.ExternalTargetId,
                    cancellationToken);
            if (existing is not null)
            {
                return ApplicationResult<LiveTargetMappingResult>.Failure(
                    LiveDataApplicationErrors.AlreadyExists());
            }

            LiveTargetReference? suggestion = null;
            if (!string.IsNullOrWhiteSpace(command.SuggestedInternalTargetId)
                && !string.IsNullOrWhiteSpace(command.SuggestedParkId))
            {
                suggestion = await this.targetResolver.ResolveAsync(
                    command.TargetType,
                    command.SuggestedInternalTargetId,
                    command.SuggestedParkId,
                    cancellationToken);
                if (suggestion is null)
                {
                    return ApplicationResult<LiveTargetMappingResult>.Failure(
                        LiveDataApplicationErrors.TargetNotFound());
                }
            }

            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            ExternalLiveTargetMapping mapping = ExternalLiveTargetMapping.CreateCandidate(
                Guid.NewGuid(),
                sourceId,
                new ExternalLiveTargetDescriptor(
                    command.TargetType,
                    command.ExternalTargetId,
                    command.ExternalParentTargetId,
                    command.ExternalDisplayName,
                    command.ExternalParentDisplayName,
                    command.ExternalCountryCode),
                suggestion,
                suggestion is null ? LiveMappingConfidence.Low : LiveMappingConfidence.Medium,
                nowUtc);
            LiveTargetMappingWriteOutcome outcome = await this.repository.AppendRevisionAsync(
                mapping,
                0,
                cancellationToken);
            return outcome == LiveTargetMappingWriteOutcome.Created
                ? ApplicationResult<LiveTargetMappingResult>.Success(
                    LiveTargetMappingResultFactory.Create(mapping))
                : ApplicationResult<LiveTargetMappingResult>.Failure(
                    LiveDataApplicationErrors.AlreadyExists());
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<LiveTargetMappingResult>.Failure(
                LiveDataApplicationErrors.InvalidMapping(exception.Message));
        }
    }
}
