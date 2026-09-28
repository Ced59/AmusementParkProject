using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class RetractHistoricalEditorialResourceCommandHandler :
    ICommandHandler<
        RetractHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>>
{
    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly TimeProvider timeProvider;

    public RetractHistoricalEditorialResourceCommandHandler(
        IHistoricalFactRepository factRepository,
        IHistoricalRelationRepository relationRepository,
        IHistoricalSourceRepository sourceRepository,
        TimeProvider? timeProvider = null)
    {
        this.factRepository = factRepository;
        this.relationRepository = relationRepository;
        this.sourceRepository = sourceRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<HistoricalEditorialMutationResult>> HandleAsync(
        RetractHistoricalEditorialResourceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            return command.ResourceType switch
            {
                HistoricalReviewResourceType.Fact => await this.RetractFactAsync(
                    command,
                    cancellationToken),
                HistoricalReviewResourceType.Source => await this.RetractSourceAsync(
                    command,
                    cancellationToken),
                HistoricalReviewResourceType.Relation => await this.RetractRelationAsync(
                    command,
                    cancellationToken),
                _ => Failure("This historical resource type cannot be retracted."),
            };
        }
        catch (ArgumentException exception)
        {
            return Failure(exception.Message);
        }
    }

    private async Task<ApplicationResult<HistoricalEditorialMutationResult>> RetractFactAsync(
        RetractHistoricalEditorialResourceCommand command,
        CancellationToken cancellationToken)
    {
        HistoricalFact? previous = await this.factRepository.GetLatestRevisionAsync(
            command.ResourceId,
            cancellationToken);
        if (previous is null)
        {
            return NotFound("fact", command.ResourceId);
        }

        if (previous.Revision != command.ExpectedRevision)
        {
            return Conflict(previous.Revision);
        }

        DateTime recordedAtUtc = this.ResolveRecordedAt(previous.RecordedAtUtc);
        HistoricalFact retraction = previous.CreateRetraction(recordedAtUtc);
        HistoricalRevisionWriteDisposition disposition =
            await this.factRepository.AppendRevisionAsync(
                retraction,
                CreateReviewEvent(command, retraction.Revision, recordedAtUtc),
                cancellationToken);
        return disposition == HistoricalRevisionWriteDisposition.Conflict
            ? Conflict(previous.Revision)
            : Success(
                HistoricalReviewResourceType.Fact,
                retraction.Id,
                retraction.Revision,
                retraction.State);
    }

    private async Task<ApplicationResult<HistoricalEditorialMutationResult>> RetractSourceAsync(
        RetractHistoricalEditorialResourceCommand command,
        CancellationToken cancellationToken)
    {
        HistoricalSourceReference? previous = await this.sourceRepository.GetLatestRevisionAsync(
            command.ResourceId,
            cancellationToken);
        if (previous is null)
        {
            return NotFound("source", command.ResourceId);
        }

        if (previous.Revision != command.ExpectedRevision)
        {
            return Conflict(previous.Revision);
        }

        DateTime recordedAtUtc = this.ResolveRecordedAt(previous.RecordedAtUtc);
        HistoricalSourceReference retraction = new HistoricalSourceReference(
            previous.Id,
            previous.Revision + 1,
            previous.Type,
            previous.Title,
            previous.PublisherOrAuthor,
            previous.Url,
            previous.BibliographicReference,
            previous.PublishedOn,
            previous.AccessedOn,
            previous.LanguageCode,
            previous.ArchiveUrl,
            previous.Scopes,
            previous.AdminNote,
            HistoricalSourceAccessibility.Withdrawn,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            recordedAtUtc,
            previous.RevisionOrigin);
        HistoricalRevisionWriteDisposition disposition =
            await this.sourceRepository.AppendRevisionAsync(
                retraction,
                CreateReviewEvent(command, retraction.Revision, recordedAtUtc),
                cancellationToken);
        return disposition == HistoricalRevisionWriteDisposition.Conflict
            ? Conflict(previous.Revision)
            : Success(
                HistoricalReviewResourceType.Source,
                retraction.Id,
                retraction.Revision,
                null);
    }

    private async Task<ApplicationResult<HistoricalEditorialMutationResult>> RetractRelationAsync(
        RetractHistoricalEditorialResourceCommand command,
        CancellationToken cancellationToken)
    {
        HistoricalRelation? previous = await this.relationRepository.GetLatestRevisionAsync(
            command.ResourceId,
            cancellationToken);
        if (previous is null)
        {
            return NotFound("relation", command.ResourceId);
        }

        if (previous.Revision != command.ExpectedRevision)
        {
            return Conflict(previous.Revision);
        }

        DateTime recordedAtUtc = this.ResolveRecordedAt(previous.RecordedAtUtc);
        HistoricalRelation retraction = previous.CreateRetraction(recordedAtUtc);
        HistoricalRevisionWriteDisposition disposition =
            await this.relationRepository.AppendRevisionAsync(
                retraction,
                CreateReviewEvent(command, retraction.Revision, recordedAtUtc),
                cancellationToken);
        return disposition == HistoricalRevisionWriteDisposition.Conflict
            ? Conflict(previous.Revision)
            : Success(
                HistoricalReviewResourceType.Relation,
                retraction.Id,
                retraction.Revision,
                retraction.State);
    }

    private HistoricalReviewEvent CreateReviewEvent(
        RetractHistoricalEditorialResourceCommand command,
        int revision,
        DateTime occurredAtUtc)
    {
        return new HistoricalReviewEvent(
            Guid.NewGuid(),
            command.ResourceType,
            command.ResourceId,
            revision,
            HistoricalReviewEventType.Retracted,
            command.ActorUserId,
            command.ReviewNote,
            occurredAtUtc);
    }

    private DateTime ResolveRecordedAt(DateTime previousRecordedAtUtc)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        return previousRecordedAtUtc > nowUtc ? previousRecordedAtUtc : nowUtc;
    }

    private static ApplicationResult<HistoricalEditorialMutationResult> NotFound(
        string resourceType,
        Guid resourceId)
    {
        return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
            HistoryApplicationErrors.EditorialResourceNotFound(resourceType, resourceId));
    }

    private static ApplicationResult<HistoricalEditorialMutationResult> Conflict(int currentRevision)
    {
        return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
            HistoryApplicationErrors.EditorialRevisionConflict(currentRevision));
    }

    private static ApplicationResult<HistoricalEditorialMutationResult> Failure(string message)
    {
        return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
            HistoryApplicationErrors.InvalidEditorialResource(message));
    }

    private static ApplicationResult<HistoricalEditorialMutationResult> Success(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int revision,
        HistoricalFactState? factState)
    {
        return ApplicationResult<HistoricalEditorialMutationResult>.Success(
            new HistoricalEditorialMutationResult(
                resourceType,
                resourceId,
                revision,
                HistoricalEditorialWorkflowState.Retracted,
                HistoricalPublicationState.Withdrawn,
                factState));
    }
}
