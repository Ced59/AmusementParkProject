using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class AdvanceHistoricalEditorialResourceCommandHandler :
    ICommandHandler<
        AdvanceHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>>
{
    private const string MethodologyVersion = "historical-editor-v1";

    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly HistoricalLineagePublicationValidator lineagePublicationValidator;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;
    private readonly TimeProvider timeProvider;

    public AdvanceHistoricalEditorialResourceCommandHandler(
        IHistoricalFactRepository factRepository,
        IHistoricalRelationRepository relationRepository,
        IHistoricalSourceRepository sourceRepository,
        HistoricalLineagePublicationValidator lineagePublicationValidator,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler,
        TimeProvider? timeProvider = null)
    {
        this.factRepository = factRepository;
        this.relationRepository = relationRepository;
        this.sourceRepository = sourceRepository;
        this.lineagePublicationValidator = lineagePublicationValidator;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<HistoricalEditorialMutationResult>> HandleAsync(
        AdvanceHistoricalEditorialResourceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            return command.ResourceType switch
            {
                HistoricalReviewResourceType.Fact => await this.AdvanceFactAsync(
                    command,
                    cancellationToken),
                HistoricalReviewResourceType.Source => await this.AdvanceSourceAsync(
                    command,
                    cancellationToken),
                HistoricalReviewResourceType.Relation => await this.AdvanceRelationAsync(
                    command,
                    cancellationToken),
                _ => ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                    HistoryApplicationErrors.InvalidEditorialResource(
                        "This historical resource type cannot use the editorial workflow.")),
            };
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.InvalidEditorialResource(exception.Message));
        }
    }

    private async Task<ApplicationResult<HistoricalEditorialMutationResult>> AdvanceFactAsync(
        AdvanceHistoricalEditorialResourceCommand command,
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

        HistoricalEditorialWorkflowState targetStage = HistoricalEditorialTransitionPolicy.GetNextStage(
            HistoricalReviewResourceType.Fact,
            previous.WorkflowState);
        DateTime recordedAtUtc = this.ResolveRecordedAt(previous.RecordedAtUtc);
        bool isPublication = targetStage == HistoricalEditorialWorkflowState.Published;
        DateTime? verifiedAtUtc = previous.State == HistoricalFactState.Verified
            && targetStage >= HistoricalEditorialWorkflowState.StructuredValidation
                ? previous.VerifiedAtUtc ?? recordedAtUtc
                : null;
        HistoricalFact fact = new HistoricalFact(
            previous.Id,
            previous.Subject,
            previous.Type,
            previous.Period,
            previous.State,
            previous.Importance,
            targetStage,
            isPublication ? HistoricalPublicationState.Published : previous.PublicationState,
            previous.PublicUncertaintyExplanation,
            previous.LifecycleBoundaryMeaning,
            previous.AttributeKind,
            previous.AttributeBoundaryMeaning,
            previous.SequenceWithinDate,
            previous.SourceReferences,
            previous.StructuredValue,
            previous.OtherTypeLabel,
            previous.NarrativeContentId,
            verifiedAtUtc,
            isPublication ? recordedAtUtc : null,
            isPublication ? MethodologyVersion : null,
            previous.Revision + 1,
            previous.Revision,
            recordedAtUtc,
            previous.RevisionOrigin);
        HistoricalRevisionWriteDisposition disposition =
            await this.factRepository.AppendRevisionAsync(
                fact,
                CreateReviewEvent(command, fact.Revision, targetStage, recordedAtUtc),
                cancellationToken);
        if (disposition == HistoricalRevisionWriteDisposition.Conflict)
        {
            return Conflict(previous.Revision);
        }

        if (isPublication)
        {
            await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        }

        return Success(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            fact.WorkflowState,
            fact.PublicationState,
            fact.State);
    }

    private async Task<ApplicationResult<HistoricalEditorialMutationResult>> AdvanceSourceAsync(
        AdvanceHistoricalEditorialResourceCommand command,
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

        HistoricalEditorialWorkflowState targetStage = HistoricalEditorialTransitionPolicy.GetNextStage(
            HistoricalReviewResourceType.Source,
            previous.WorkflowState);
        DateTime recordedAtUtc = this.ResolveRecordedAt(previous.RecordedAtUtc);
        HistoricalSourceReference source = new HistoricalSourceReference(
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
            previous.Accessibility,
            targetStage,
            targetStage == HistoricalEditorialWorkflowState.Published
                ? HistoricalPublicationState.Published
                : previous.PublicationState,
            recordedAtUtc,
            previous.RevisionOrigin);
        HistoricalRevisionWriteDisposition disposition =
            await this.sourceRepository.AppendRevisionAsync(
                source,
                CreateReviewEvent(command, source.Revision, targetStage, recordedAtUtc),
                cancellationToken);
        return disposition == HistoricalRevisionWriteDisposition.Conflict
            ? Conflict(previous.Revision)
            : Success(
                HistoricalReviewResourceType.Source,
                source.Id,
                source.Revision,
                source.WorkflowState,
                source.PublicationState,
                null);
    }

    private async Task<ApplicationResult<HistoricalEditorialMutationResult>> AdvanceRelationAsync(
        AdvanceHistoricalEditorialResourceCommand command,
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

        HistoricalEditorialWorkflowState targetStage = HistoricalEditorialTransitionPolicy.GetNextStage(
            HistoricalReviewResourceType.Relation,
            previous.WorkflowState);
        DateTime recordedAtUtc = this.ResolveRecordedAt(previous.RecordedAtUtc);
        bool isPublication = targetStage == HistoricalEditorialWorkflowState.Published;
        DateTime? verifiedAtUtc = previous.State == HistoricalFactState.Verified
            && targetStage >= HistoricalEditorialWorkflowState.StructuredValidation
                ? previous.VerifiedAtUtc ?? recordedAtUtc
                : null;
        HistoricalRelation relation = new HistoricalRelation(
            previous.Id,
            previous.Source,
            previous.Target,
            previous.Type,
            previous.Direction,
            previous.Period,
            previous.State,
            targetStage,
            isPublication ? HistoricalPublicationState.Published : previous.PublicationState,
            previous.PublicUncertaintyExplanation,
            previous.SourceReferences,
            previous.EditorialNote,
            verifiedAtUtc,
            isPublication ? recordedAtUtc : null,
            isPublication ? MethodologyVersion : null,
            previous.Revision + 1,
            previous.Revision,
            recordedAtUtc,
            previous.RevisionOrigin);
        if (isPublication
            && await this.lineagePublicationValidator.WouldCreateCycleAsync(
                relation,
                cancellationToken))
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.IncompatibleLineageCycle());
        }

        HistoricalRevisionWriteDisposition disposition =
            await this.relationRepository.AppendRevisionAsync(
                relation,
                CreateReviewEvent(command, relation.Revision, targetStage, recordedAtUtc),
                cancellationToken);
        return disposition == HistoricalRevisionWriteDisposition.Conflict
            ? Conflict(previous.Revision)
            : Success(
                HistoricalReviewResourceType.Relation,
                relation.Id,
                relation.Revision,
                relation.WorkflowState,
                relation.PublicationState,
                relation.State);
    }

    private HistoricalReviewEvent CreateReviewEvent(
        AdvanceHistoricalEditorialResourceCommand command,
        int revision,
        HistoricalEditorialWorkflowState targetStage,
        DateTime occurredAtUtc)
    {
        return new HistoricalReviewEvent(
            Guid.NewGuid(),
            command.ResourceType,
            command.ResourceId,
            revision,
            HistoricalEditorialTransitionPolicy.GetAdvanceEventType(targetStage),
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

    private static ApplicationResult<HistoricalEditorialMutationResult> Success(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int revision,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalFactState? factState)
    {
        return ApplicationResult<HistoricalEditorialMutationResult>.Success(
            new HistoricalEditorialMutationResult(
                resourceType,
                resourceId,
                revision,
                workflowState,
                publicationState,
                factState));
    }
}
