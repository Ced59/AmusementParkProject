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

public sealed class SaveHistoricalFactCommandHandler :
    ICommandHandler<
        SaveHistoricalFactCommand,
        ApplicationResult<HistoricalEditorialMutationResult>>
{
    private const string MethodologyVersion = "historical-editor-v1";

    private readonly HistoricalParkEditorialScopeLoader scopeLoader;
    private readonly HistoricalParkEditorialSubjectResolver subjectResolver;
    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly TimeProvider timeProvider;

    public SaveHistoricalFactCommandHandler(
        HistoricalParkEditorialScopeLoader scopeLoader,
        HistoricalParkEditorialSubjectResolver subjectResolver,
        IHistoricalFactRepository factRepository,
        IHistoricalSourceRepository sourceRepository,
        TimeProvider? timeProvider = null)
    {
        this.scopeLoader = scopeLoader;
        this.subjectResolver = subjectResolver;
        this.factRepository = factRepository;
        this.sourceRepository = sourceRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<HistoricalEditorialMutationResult>> HandleAsync(
        SaveHistoricalFactCommand command,
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

        HistoricalFact? previous = command.FactId.HasValue
            ? await this.factRepository.GetLatestRevisionAsync(command.FactId.Value, cancellationToken)
            : null;
        if (command.FactId.HasValue && previous is null)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialResourceNotFound("fact", command.FactId.Value));
        }

        if (previous is not null && !BelongsToPark(previous.Subject, scope.ParkId))
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialResourceNotFound("fact", previous.Id));
        }

        if (previous is not null && command.ExpectedRevision != previous.Revision)
        {
            return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                HistoryApplicationErrors.EditorialRevisionConflict(previous.Revision));
        }

        try
        {
            HistoricalFactDraftInput draft = command.Draft;
            IReadOnlyCollection<HistoricalSubject> editableSubjects = previous is null
                ? await this.subjectResolver.LoadAsync(scope, cancellationToken)
                : scope.CurrentSubjects;
            HistoricalSubject subject = previous?.Subject
                ?? HistoricalEditorialInputMapper.ResolveSubject(
                    editableSubjects,
                    draft.SubjectType,
                    draft.SubjectId,
                    scope.ParkId);
            if (subject.Type != draft.SubjectType
                || !string.Equals(subject.Id, draft.SubjectId.Trim(), StringComparison.Ordinal))
            {
                throw new HistoricalPersistenceValidationException(
                    HistoricalPersistenceErrorCodes.InvalidIdentifier,
                    "A historical fact cannot change subject during revision.");
            }

            HistoricalPeriod period = HistoricalEditorialInputMapper.ToPeriod(draft.Period);
            IReadOnlyCollection<HistoricalSourceReference> sources =
                await this.LoadSourcesAsync(draft.Sources, cancellationToken);
            HistoricalSourceRevisionReference[] sourceReferences =
                HistoricalEditorialInputMapper.ToFactSourceReferences(
                    draft,
                    subject,
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
                : previous?.PublicationState ?? HistoricalPublicationState.Draft;
            DateTime? verifiedAtUtc = draft.State == HistoricalFactState.Verified
                && workflowState >= HistoricalEditorialWorkflowState.StructuredValidation
                    ? previous?.VerifiedAtUtc ?? recordedAtUtc
                    : null;
            DateTime? publishedAtUtc = isCorrection
                ? previous?.PublishedAtUtc ?? recordedAtUtc
                : null;
            HistoricalFact fact = new HistoricalFact(
                previous?.Id ?? Guid.NewGuid(),
                subject,
                draft.Type,
                period,
                draft.State,
                draft.Importance,
                workflowState,
                publicationState,
                draft.PublicUncertaintyExplanation,
                draft.LifecycleBoundaryMeaning,
                draft.AttributeKind,
                draft.AttributeBoundaryMeaning,
                draft.SequenceWithinDate,
                sourceReferences,
                draft.StructuredValue,
                draft.OtherTypeLabel,
                draft.NarrativeContentId,
                verifiedAtUtc,
                publishedAtUtc,
                isCorrection ? previous?.PublicationMethodologyVersion ?? MethodologyVersion : null,
                (previous?.Revision ?? 0) + 1,
                previous?.Revision,
                recordedAtUtc,
                previous?.RevisionOrigin ?? HistoricalRevisionOrigin.Ordinary);
            HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
                Guid.NewGuid(),
                HistoricalReviewResourceType.Fact,
                fact.Id,
                fact.Revision,
                previous is null
                    ? HistoricalReviewEventType.Created
                    : HistoricalEditorialTransitionPolicy.GetSaveEventType(previous.WorkflowState),
                command.ActorUserId,
                command.ReviewNote,
                recordedAtUtc);
            HistoricalRevisionWriteDisposition disposition =
                await this.factRepository.AppendRevisionAsync(
                    fact,
                    reviewEvent,
                    cancellationToken);
            if (disposition == HistoricalRevisionWriteDisposition.Conflict)
            {
                return ApplicationResult<HistoricalEditorialMutationResult>.Failure(
                    HistoryApplicationErrors.EditorialRevisionConflict(previous?.Revision ?? 0));
            }

            return ApplicationResult<HistoricalEditorialMutationResult>.Success(
                ToResult(fact));
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

    private static bool BelongsToPark(HistoricalSubject subject, string parkId)
    {
        return string.Equals(subject.ContextParkId, parkId, StringComparison.Ordinal)
            || subject.Type == HistoricalSubjectType.Park
                && string.Equals(subject.Id, parkId, StringComparison.Ordinal);
    }

    private static HistoricalEditorialMutationResult ToResult(HistoricalFact fact)
    {
        return new HistoricalEditorialMutationResult(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            fact.WorkflowState,
            fact.PublicationState,
            fact.State);
    }
}
