using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class SaveHistoricalRelationCommandHandler :
    ICommandHandler<
        SaveHistoricalRelationCommand,
        ApplicationResult<HistoricalEditorialMutationResult>>
{
    private const string MethodologyVersion = "historical-editor-v1";

    private readonly HistoricalParkEditorialScopeLoader scopeLoader;
    private readonly HistoricalParkEditorialSubjectResolver subjectResolver;
    private readonly HistoricalLineagePublicationValidator lineagePublicationValidator;
    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly TimeProvider timeProvider;

    public SaveHistoricalRelationCommandHandler(
        HistoricalParkEditorialScopeLoader scopeLoader,
        HistoricalParkEditorialSubjectResolver subjectResolver,
        HistoricalLineagePublicationValidator lineagePublicationValidator,
        IHistoricalRelationRepository relationRepository,
        IHistoricalSourceRepository sourceRepository,
        TimeProvider? timeProvider = null)
    {
        this.scopeLoader = scopeLoader;
        this.subjectResolver = subjectResolver;
        this.lineagePublicationValidator = lineagePublicationValidator;
        this.relationRepository = relationRepository;
        this.sourceRepository = sourceRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<HistoricalEditorialMutationResult>> HandleAsync(
        SaveHistoricalRelationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Draft);
        HistoricalParkEditorialScope? scope = await this.scopeLoader.LoadAsync(
            command.ParkId,
            cancellationToken);
        if (scope is null)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), command.ParkId));
        }

        HistoricalRelation? previous = command.RelationId.HasValue
            ? await this.relationRepository.GetLatestRevisionAsync(
                command.RelationId.Value,
                cancellationToken)
            : null;
        if (command.RelationId.HasValue && previous is null)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialResourceNotFound(
                    "relation",
                    command.RelationId.Value));
        }

        if (previous is not null && !TouchesPark(previous, scope.ParkId))
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialResourceNotFound("relation", previous.Id));
        }

        if (previous is not null && command.ExpectedRevision != previous.Revision)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialRevisionConflict(previous.Revision));
        }

        try
        {
            HistoricalRelationDraftInput draft = command.Draft;
            IReadOnlyCollection<HistoricalSubject> editableSubjects = previous is null
                ? await this.subjectResolver.LoadAsync(scope, cancellationToken)
                : scope.CurrentSubjects;
            HistoricalSubject sourceSubject = previous?.Source
                ?? HistoricalEditorialInputMapper.ResolveSubject(
                    editableSubjects,
                    draft.SourceSubjectType,
                    draft.SourceSubjectId,
                    draft.SourceSubjectContextParkId);
            HistoricalSubject targetSubject = previous?.Target
                ?? HistoricalEditorialInputMapper.ResolveSubject(
                    editableSubjects,
                    draft.TargetSubjectType,
                    draft.TargetSubjectId,
                    draft.TargetSubjectContextParkId);
            EnsureSubjectUnchanged(
                sourceSubject,
                draft.SourceSubjectType,
                draft.SourceSubjectId,
                draft.SourceSubjectContextParkId);
            EnsureSubjectUnchanged(
                targetSubject,
                draft.TargetSubjectType,
                draft.TargetSubjectId,
                draft.TargetSubjectContextParkId);
            HistoricalPeriod period = HistoricalEditorialInputMapper.ToPeriod(draft.Period);
            IReadOnlyCollection<HistoricalSourceReference> sources =
                await this.LoadSourcesAsync(draft.Sources, cancellationToken);
            HistoricalRelationSourceRevisionReference[] sourceReferences =
                HistoricalEditorialInputMapper.ToRelationSourceReferences(
                    draft,
                    sourceSubject,
                    targetSubject,
                    period,
                    sources);
            DateTime recordedAtUtc = this.ResolveRecordedAt(previous?.RecordedAtUtc);
            bool isCorrection = previous?.WorkflowState is HistoricalEditorialWorkflowState.Published
                or HistoricalEditorialWorkflowState.Corrected;
            HistoricalEditorialWorkflowState workflowState = isCorrection
                ? HistoricalEditorialWorkflowState.Corrected
                : previous?.WorkflowState ?? HistoricalEditorialWorkflowState.Draft;
            HistoricalPublicationState publicationState = isCorrection
                ? HistoricalPublicationState.Published
                : HistoricalPublicationState.Draft;
            DateTime? verifiedAtUtc = draft.State == HistoricalFactState.Verified
                && workflowState >= HistoricalEditorialWorkflowState.StructuredValidation
                    ? previous?.VerifiedAtUtc ?? recordedAtUtc
                    : null;
            HistoricalRelation relation = new HistoricalRelation(
                previous?.Id ?? Guid.NewGuid(),
                sourceSubject,
                targetSubject,
                draft.Type,
                draft.Direction,
                period,
                draft.State,
                workflowState,
                publicationState,
                draft.PublicUncertaintyExplanation,
                sourceReferences,
                draft.EditorialNote,
                verifiedAtUtc,
                isCorrection ? previous?.PublishedAtUtc ?? recordedAtUtc : null,
                isCorrection ? previous?.PublicationMethodologyVersion ?? MethodologyVersion : null,
                (previous?.Revision ?? 0) + 1,
                previous?.Revision,
                recordedAtUtc);
            if (relation.PublicationState == HistoricalPublicationState.Published
                && await this.lineagePublicationValidator.WouldCreateCycleAsync(
                    relation,
                    cancellationToken))
            {
                return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                    HistoryApplicationErrors.IncompatibleLineageCycle());
            }

            HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
                Guid.NewGuid(),
                HistoricalReviewResourceType.Relation,
                relation.Id,
                relation.Revision,
                previous is null
                    ? HistoricalReviewEventType.Created
                    : HistoricalEditorialTransitionPolicy.GetSaveEventType(previous.WorkflowState),
                command.ActorUserId,
                command.ReviewNote,
                recordedAtUtc);
            HistoricalRevisionWriteDisposition disposition =
                await this.relationRepository.AppendRevisionAsync(
                    relation,
                    reviewEvent,
                    cancellationToken);
            if (disposition == HistoricalRevisionWriteDisposition.Conflict)
            {
                return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                    HistoryApplicationErrors.EditorialRevisionConflict(previous?.Revision ?? 0));
            }

            return ApplicationResult<HistoricalEditorialMutationResult>.Success(
                new HistoricalEditorialMutationResult(
                    HistoricalReviewResourceType.Relation,
                    relation.Id,
                    relation.Revision,
                    relation.WorkflowState,
                    relation.PublicationState,
                    relation.State));
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.InvalidEditorialResource(exception.Message));
        }
    }

    private async Task<IReadOnlyCollection<HistoricalSourceReference>> LoadSourcesAsync(
        IReadOnlyCollection<HistoricalEvidenceSourceInput> references,
        CancellationToken cancellationToken)
    {
        HistoricalSourceRevisionKey[] sourceRevisions = references
            .Select(static reference => new HistoricalSourceRevisionKey(
                reference.SourceId,
                reference.Revision))
            .Distinct()
            .ToArray();
        return await this.sourceRepository.GetRevisionsAsync(sourceRevisions, cancellationToken);
    }

    private DateTime ResolveRecordedAt(DateTime? previousRecordedAtUtc)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        return previousRecordedAtUtc.HasValue && previousRecordedAtUtc.Value > nowUtc
            ? previousRecordedAtUtc.Value
            : nowUtc;
    }

    private static void EnsureSubjectUnchanged(
        HistoricalSubject subject,
        HistoricalSubjectType expectedType,
        string expectedId,
        string? expectedContextParkId)
    {
        if (subject.Type != expectedType
            || !string.Equals(subject.Id, expectedId.Trim(), StringComparison.Ordinal))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical relation cannot change its subjects during revision.");
        }

        if (!string.IsNullOrWhiteSpace(expectedContextParkId)
            && !string.Equals(
                subject.ContextParkId,
                expectedContextParkId.Trim(),
                StringComparison.Ordinal))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical relation cannot change its subject park contexts during revision.");
        }
    }

    private static bool TouchesPark(HistoricalRelation relation, string parkId)
    {
        return BelongsToPark(relation.Source, parkId) || BelongsToPark(relation.Target, parkId);
    }

    private static bool BelongsToPark(HistoricalSubject subject, string parkId)
    {
        return string.Equals(subject.ContextParkId, parkId, StringComparison.Ordinal)
            || subject.Type == HistoricalSubjectType.Park
                && string.Equals(subject.Id, parkId, StringComparison.Ordinal);
    }
}
