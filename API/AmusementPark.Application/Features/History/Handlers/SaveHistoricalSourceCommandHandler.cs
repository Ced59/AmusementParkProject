using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class SaveHistoricalSourceCommandHandler :
    ICommandHandler<
        SaveHistoricalSourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>>
{
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly TimeProvider timeProvider;

    public SaveHistoricalSourceCommandHandler(
        IHistoricalSourceRepository sourceRepository,
        TimeProvider? timeProvider = null)
    {
        this.sourceRepository = sourceRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<HistoricalEditorialMutationResult>> HandleAsync(
        SaveHistoricalSourceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Draft);
        HistoricalSourceReference? previous = command.SourceId.HasValue
            ? await this.sourceRepository.GetLatestRevisionAsync(
                command.SourceId.Value,
                cancellationToken)
            : null;
        if (command.SourceId.HasValue && previous is null)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialResourceNotFound(
                    "source",
                    command.SourceId.Value));
        }

        if (previous is not null && command.ExpectedRevision != previous.Revision)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialRevisionConflict(previous.Revision));
        }

        try
        {
            DateTime recordedAtUtc = ResolveRecordedAt(previous?.RecordedAtUtc);
            HistoricalEditorialWorkflowState workflowState = previous is null
                ? HistoricalEditorialWorkflowState.Draft
                : previous.WorkflowState is HistoricalEditorialWorkflowState.Published
                    or HistoricalEditorialWorkflowState.Corrected
                    ? HistoricalEditorialWorkflowState.Corrected
                    : previous.WorkflowState;
            HistoricalPublicationState publicationState = workflowState
                == HistoricalEditorialWorkflowState.Corrected
                    ? HistoricalPublicationState.Published
                    : previous?.PublicationState ?? HistoricalPublicationState.Draft;
            HistoricalSourceReference source = new HistoricalSourceReference(
                previous?.Id ?? Guid.NewGuid(),
                (previous?.Revision ?? 0) + 1,
                command.Draft.Type,
                command.Draft.Title,
                command.Draft.PublisherOrAuthor,
                command.Draft.Url,
                command.Draft.BibliographicReference,
                command.Draft.PublishedOn,
                command.Draft.AccessedOn,
                command.Draft.LanguageCode,
                command.Draft.ArchiveUrl,
                command.Draft.Scopes,
                command.Draft.AdminNote,
                command.Draft.Accessibility,
                workflowState,
                publicationState,
                recordedAtUtc,
                previous?.RevisionOrigin ?? HistoricalRevisionOrigin.Ordinary);
            HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
                Guid.NewGuid(),
                HistoricalReviewResourceType.Source,
                source.Id,
                source.Revision,
                previous is null
                    ? HistoricalReviewEventType.Created
                    : HistoricalEditorialTransitionPolicy.GetSaveEventType(previous.WorkflowState),
                command.ActorUserId,
                command.ReviewNote,
                recordedAtUtc);
            HistoricalRevisionWriteDisposition disposition =
                await this.sourceRepository.AppendRevisionAsync(
                    source,
                    reviewEvent,
                    cancellationToken);
            if (disposition == HistoricalRevisionWriteDisposition.Conflict)
            {
                return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                    HistoryApplicationErrors.EditorialRevisionConflict(previous?.Revision ?? 0));
            }

            return ApplicationResult<HistoricalEditorialMutationResult>.Success(
                new HistoricalEditorialMutationResult(
                    HistoricalReviewResourceType.Source,
                    source.Id,
                    source.Revision,
                    source.WorkflowState,
                    source.PublicationState,
                    null));
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.InvalidEditorialResource(exception.Message));
        }
    }

    private DateTime ResolveRecordedAt(DateTime? previousRecordedAtUtc)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        return previousRecordedAtUtc.HasValue && previousRecordedAtUtc.Value > nowUtc
            ? previousRecordedAtUtc.Value
            : nowUtc;
    }
}
