using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class PreviewHistoricalSnapshotImpactQueryHandler :
    IQueryHandler<
        PreviewHistoricalSnapshotImpactQuery,
        ApplicationResult<HistoricalPublicationImpactPreviewResult>>
{
    private const string MethodologyVersion = "historical-editor-v1";

    private readonly HistoricalParkEditorialScopeLoader scopeLoader;
    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly IHistoricalVisitDiagnosticsReader visitDiagnosticsReader;
    private readonly IParkHistoricalSnapshotBuilder snapshotBuilder;
    private readonly TimeProvider timeProvider;

    public PreviewHistoricalSnapshotImpactQueryHandler(
        HistoricalParkEditorialScopeLoader scopeLoader,
        IHistoricalFactRepository factRepository,
        IHistoricalRelationRepository relationRepository,
        IHistoricalSourceRepository sourceRepository,
        IHistoricalVisitDiagnosticsReader visitDiagnosticsReader,
        IParkHistoricalSnapshotBuilder snapshotBuilder,
        TimeProvider? timeProvider = null)
    {
        this.scopeLoader = scopeLoader;
        this.factRepository = factRepository;
        this.relationRepository = relationRepository;
        this.sourceRepository = sourceRepository;
        this.visitDiagnosticsReader = visitDiagnosticsReader;
        this.snapshotBuilder = snapshotBuilder;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<HistoricalPublicationImpactPreviewResult>> HandleAsync(
        PreviewHistoricalSnapshotImpactQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<HistoricalPublicationImpactPreviewResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        HistoricalParkEditorialScope? scope = await this.scopeLoader.LoadAsync(
            query.ParkId,
            cancellationToken);
        if (scope is null)
        {
            return ApplicationResult<HistoricalPublicationImpactPreviewResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), query.ParkId));
        }

        return query.ResourceType switch
        {
            HistoricalReviewResourceType.Fact => await this.PreviewFactAsync(
                scope,
                query,
                cancellationToken),
            HistoricalReviewResourceType.Relation => await this.PreviewRelationAsync(
                scope,
                query,
                cancellationToken),
            _ => ApplicationResult<HistoricalPublicationImpactPreviewResult>.Failure(
                HistoryApplicationErrors.InvalidEditorialResource(
                    "Only facts and relations can change historical park snapshots or lineages.")),
        };
    }

    private async Task<ApplicationResult<HistoricalPublicationImpactPreviewResult>> PreviewFactAsync(
        HistoricalParkEditorialScope scope,
        PreviewHistoricalSnapshotImpactQuery query,
        CancellationToken cancellationToken)
    {
        HistoricalFact? candidate = await this.factRepository.GetLatestRevisionAsync(
            query.ResourceId,
            cancellationToken);
        if (candidate is null || !BelongsToPark(candidate.Subject, scope.ParkId))
        {
            return NotFound("fact", query.ResourceId);
        }

        IReadOnlyCollection<HistoricalFact> currentFacts =
            await this.factRepository.GetLatestRevisionsForParkAsync(
                scope.ParkId,
                scope.CurrentSubjects,
                cancellationToken);
        IReadOnlyCollection<HistoricalSourceReference> sources =
            await this.sourceRepository.GetRevisionsAsync(
                candidate.SourceReferences,
                cancellationToken);
        List<string> blockers = new List<string>();
        HistoricalFact? publishedCandidate = null;
        try
        {
            publishedCandidate = ToPublished(candidate);
            HistoricalFactEvidenceValidator.Validate(publishedCandidate, sources);
        }
        catch (ArgumentException exception)
        {
            blockers.Add(exception is HistoricalPersistenceValidationException validation
                ? validation.ErrorCode
                : "history.editorial.preview.invalid");
        }

        int previewYear = ResolvePreviewYear(query.Year, candidate.Period);
        HistoricalSubject[] subjects = scope.CurrentSubjects
            .Append(candidate.Subject)
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .ToArray();
        ParkHistoricalSnapshot before = this.snapshotBuilder.Build(
            scope.ParkId,
            HistoricalInstant.ForYear(previewYear),
            subjects,
            currentFacts);
        IReadOnlyCollection<HistoricalFact> proposedFacts = publishedCandidate is null
            ? currentFacts
            : currentFacts
                .Where(fact => fact.Id != publishedCandidate.Id)
                .Append(publishedCandidate)
                .ToArray();
        ParkHistoricalSnapshot after = this.snapshotBuilder.Build(
            scope.ParkId,
            HistoricalInstant.ForYear(previewYear),
            subjects,
            proposedFacts);
        HistoricalDateEnvelope envelope = candidate.Period.GetPossibleEnvelope();
        HistoricalVisitDiagnosticCounts visits =
            await this.visitDiagnosticsReader.GetCountsAsync(scope.ParkId, cancellationToken);

        return ApplicationResult<HistoricalPublicationImpactPreviewResult>.Success(
            new HistoricalPublicationImpactPreviewResult(
                HistoricalReviewResourceType.Fact,
                candidate.Id,
                string.Concat(candidate.Subject.HistoricalLabel, " · ", candidate.Type),
                previewYear,
                blockers.Count == 0,
                blockers,
                envelope.EarliestPossibleDate?.Year,
                envelope.LatestPossibleDate?.Year,
                CountAffectedYears(envelope),
                CountChangedSubjects(before, after),
                ToSummary(before),
                ToSummary(after),
                visits));
    }

    private async Task<ApplicationResult<HistoricalPublicationImpactPreviewResult>> PreviewRelationAsync(
        HistoricalParkEditorialScope scope,
        PreviewHistoricalSnapshotImpactQuery query,
        CancellationToken cancellationToken)
    {
        HistoricalRelation? candidate = await this.relationRepository.GetLatestRevisionAsync(
            query.ResourceId,
            cancellationToken);
        if (candidate is null
            || !BelongsToPark(candidate.Source, scope.ParkId)
                && !BelongsToPark(candidate.Target, scope.ParkId))
        {
            return NotFound("relation", query.ResourceId);
        }

        IReadOnlyCollection<HistoricalSourceReference> sources =
            await this.sourceRepository.GetRevisionsAsync(
                candidate.SourceReferences,
                cancellationToken);
        List<string> blockers = new List<string>();
        try
        {
            HistoricalRelation publishedCandidate = ToPublished(candidate);
            HistoricalRelationEvidenceValidator.Validate(publishedCandidate, sources);
        }
        catch (ArgumentException exception)
        {
            blockers.Add(exception is HistoricalPersistenceValidationException validation
                ? validation.ErrorCode
                : "history.editorial.preview.invalid");
        }

        IReadOnlyCollection<HistoricalFact> facts =
            await this.factRepository.GetLatestRevisionsForParkAsync(
                scope.ParkId,
                scope.CurrentSubjects,
                cancellationToken);
        HistoricalSubject[] subjects = scope.CurrentSubjects
            .Append(candidate.Source)
            .Append(candidate.Target)
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .ToArray();
        int previewYear = ResolvePreviewYear(query.Year, candidate.Period);
        ParkHistoricalSnapshot snapshot = this.snapshotBuilder.Build(
            scope.ParkId,
            HistoricalInstant.ForYear(previewYear),
            subjects,
            facts);
        HistoricalDateEnvelope envelope = candidate.Period.GetPossibleEnvelope();
        HistoricalVisitDiagnosticCounts visits =
            await this.visitDiagnosticsReader.GetCountsAsync(scope.ParkId, cancellationToken);
        HistoricalSnapshotImpactSummary summary = ToSummary(snapshot);
        return ApplicationResult<HistoricalPublicationImpactPreviewResult>.Success(
            new HistoricalPublicationImpactPreviewResult(
                HistoricalReviewResourceType.Relation,
                candidate.Id,
                string.Concat(
                    candidate.Source.HistoricalLabel,
                    " → ",
                    candidate.Target.HistoricalLabel),
                previewYear,
                blockers.Count == 0,
                blockers,
                envelope.EarliestPossibleDate?.Year,
                envelope.LatestPossibleDate?.Year,
                CountAffectedYears(envelope),
                0,
                summary,
                summary,
                visits));
    }

    private HistoricalFact ToPublished(HistoricalFact candidate)
    {
        DateTime nowUtc = this.ResolveRecordedAt(candidate.RecordedAtUtc);
        return new HistoricalFact(
            candidate.Id,
            candidate.Subject,
            candidate.Type,
            candidate.Period,
            candidate.State,
            candidate.Importance,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            candidate.PublicUncertaintyExplanation,
            candidate.LifecycleBoundaryMeaning,
            candidate.AttributeKind,
            candidate.AttributeBoundaryMeaning,
            candidate.SequenceWithinDate,
            candidate.SourceReferences,
            candidate.StructuredValue,
            candidate.OtherTypeLabel,
            candidate.NarrativeContentId,
            candidate.State == HistoricalFactState.Verified
                ? candidate.VerifiedAtUtc ?? nowUtc
                : null,
            nowUtc,
            MethodologyVersion,
            candidate.Revision + 1,
            candidate.Revision,
            nowUtc,
            candidate.RevisionOrigin);
    }

    private HistoricalRelation ToPublished(HistoricalRelation candidate)
    {
        DateTime nowUtc = this.ResolveRecordedAt(candidate.RecordedAtUtc);
        return new HistoricalRelation(
            candidate.Id,
            candidate.Source,
            candidate.Target,
            candidate.Type,
            candidate.Direction,
            candidate.Period,
            candidate.State,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            candidate.PublicUncertaintyExplanation,
            candidate.SourceReferences,
            candidate.EditorialNote,
            candidate.State == HistoricalFactState.Verified
                ? candidate.VerifiedAtUtc ?? nowUtc
                : null,
            nowUtc,
            MethodologyVersion,
            candidate.Revision + 1,
            candidate.Revision,
            nowUtc,
            candidate.RevisionOrigin);
    }

    private DateTime ResolveRecordedAt(DateTime previousRecordedAtUtc)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        return previousRecordedAtUtc > nowUtc ? previousRecordedAtUtc : nowUtc;
    }

    private int ResolvePreviewYear(int? requestedYear, HistoricalPeriod period)
    {
        return requestedYear
            ?? period.Start?.Year
            ?? period.End?.Year
            ?? this.timeProvider.GetUtcNow().Year;
    }

    private static int? CountAffectedYears(HistoricalDateEnvelope envelope)
    {
        if (!envelope.EarliestPossibleDate.HasValue || !envelope.LatestPossibleDate.HasValue)
        {
            return null;
        }

        return envelope.LatestPossibleDate.Value.Year
            - envelope.EarliestPossibleDate.Value.Year
            + 1;
    }

    private static int CountChangedSubjects(
        ParkHistoricalSnapshot before,
        ParkHistoricalSnapshot after)
    {
        Dictionary<(HistoricalSubjectType Type, string Id), string> beforeStates = before.Subjects
            .ToDictionary(
                static subject => (subject.Subject.Type, subject.Subject.Id),
                BuildSnapshotFingerprint);
        return after.Subjects.Count(subject =>
            !beforeStates.TryGetValue((subject.Subject.Type, subject.Subject.Id), out string? state)
            || !string.Equals(state, BuildSnapshotFingerprint(subject), StringComparison.Ordinal));
    }

    private static string BuildSnapshotFingerprint(HistoricalSubjectSnapshot snapshot)
    {
        string attributes = string.Join(
            '|',
            snapshot.Attributes.Select(attribute => string.Concat(
                attribute.Kind,
                ':',
                attribute.State,
                ':',
                attribute.Value)));
        return string.Concat(
            snapshot.OperationalState,
            ';',
            snapshot.PresenceExtent,
            ';',
            attributes);
    }

    private static HistoricalSnapshotImpactSummary ToSummary(ParkHistoricalSnapshot snapshot)
    {
        return new HistoricalSnapshotImpactSummary(
            snapshot.Subjects.Count(static subject =>
                subject.OperationalState == HistoricalOperationalState.KnownOpen),
            snapshot.Ambiguities.Count,
            snapshot.Coverage.ReliablePeriodSubjectCount,
            snapshot.Coverage.PartialPeriodSubjectCount,
            snapshot.Coverage.UndatedSubjectCount);
    }

    private static bool BelongsToPark(HistoricalSubject subject, string parkId)
    {
        return string.Equals(subject.ContextParkId, parkId, StringComparison.Ordinal)
            || subject.Type == HistoricalSubjectType.Park
                && string.Equals(subject.Id, parkId, StringComparison.Ordinal);
    }

    private static ApplicationResult<HistoricalPublicationImpactPreviewResult> NotFound(
        string resourceType,
        Guid resourceId)
    {
        return ApplicationResult<HistoricalPublicationImpactPreviewResult>.Failure(
            HistoryApplicationErrors.EditorialResourceNotFound(resourceType, resourceId));
    }
}
