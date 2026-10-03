using System.Globalization;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

internal sealed class HistoricalNarrativeCanonicalRevisionWriter
{
    private const int MaximumWriteAttempts = 3;

    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly HistoricalNarrativeCanonicalSourcePlanner sourcePlanner;

    public HistoricalNarrativeCanonicalRevisionWriter(
        IHistoricalFactRepository factRepository,
        IHistoricalSourceRepository sourceRepository,
        HistoricalNarrativeCanonicalSourcePlanner sourcePlanner)
    {
        this.factRepository = factRepository ?? throw new ArgumentNullException(nameof(factRepository));
        this.sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        this.sourcePlanner = sourcePlanner ?? throw new ArgumentNullException(nameof(sourcePlanner));
    }

    internal async Task PersistSourceAsync(
        HistoricalSourcePlan plan,
        CancellationToken cancellationToken)
    {
        HistoricalSourceReference latest = plan.DraftSource;
        await this.AppendSourceAsync(
            latest,
            HistoricalReviewEventType.Created,
            "Création de la source HIST canonique depuis la référence éditoriale.",
            cancellationToken);
        while (latest.Revision < plan.PublishedSource.Revision)
        {
            HistoricalEditorialWorkflowState target = HistoricalEditorialTransitionPolicy.GetNextStage(
                HistoricalReviewResourceType.Source,
                latest.WorkflowState);
            latest = this.sourcePlanner.CreateRevision(latest, target, latest.RecordedAtUtc);
            await this.AppendSourceAsync(
                latest,
                HistoricalEditorialTransitionPolicy.GetAdvanceEventType(target),
                "Conservation de la publication éditoriale existante pendant la migration canonique.",
                cancellationToken);
        }
    }

    internal async Task<bool> NeedsCanonicalRepairAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        if (!historyEvent.CanonicalFactId.HasValue)
        {
            return true;
        }

        HistoricalFact? fact = await this.factRepository.GetLatestRevisionAsync(
            historyEvent.CanonicalFactId.Value,
            cancellationToken);
        if (fact is null)
        {
            return true;
        }

        if (!historyEvent.IsVisible
            || fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        if (fact.PublicationState == HistoricalPublicationState.Withdrawn)
        {
            return false;
        }

        if (!fact.IsPublicTimelineEligible || fact.SourceReferences.Count == 0)
        {
            return true;
        }

        IReadOnlyCollection<HistoricalSourceReference> resolvedSources =
            await this.sourceRepository.GetRevisionsAsync(
                fact.SourceReferences,
                cancellationToken);
        HashSet<(Guid SourceId, int Revision)> expectedSourceRevisions = fact.SourceReferences
            .Select(static reference => (reference.SourceId, reference.Revision))
            .ToHashSet();
        HashSet<(Guid SourceId, int Revision)> resolvedSourceRevisions = resolvedSources
            .Select(static source => (source.Id, source.Revision))
            .ToHashSet();
        if (!expectedSourceRevisions.SetEquals(resolvedSourceRevisions))
        {
            return true;
        }

        IReadOnlyCollection<HistoricalSourceReference> latestSources =
            await this.sourceRepository.GetLatestRevisionsAsync(
                resolvedSources.Select(static source => source.Id).Distinct().ToArray(),
                cancellationToken);
        if (latestSources.Any(static source =>
                source.PublicationState == HistoricalPublicationState.Withdrawn))
        {
            return false;
        }

        IReadOnlySet<(Guid SourceId, int Revision)> admissibleSourceRevisions =
            HistoricalRelationEvidenceValidator.FilterCurrentlyAdmissiblePublicSources(
                    resolvedSources,
                    latestSources)
                .Select(static source => (source.Id, source.Revision))
                .ToHashSet();
        return !HistoricalFactEvidenceValidator.HasCurrentlyAdmissiblePublicEvidence(
            fact,
            admissibleSourceRevisions);
    }

    internal async Task AppendFactAsync(
        HistoricalFact fact,
        HistoricalReviewEventType eventType,
        string note,
        CancellationToken cancellationToken)
    {
        HistoricalReviewEvent reviewEvent = CreateReviewEvent(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            eventType,
            note,
            fact.RecordedAtUtc);
        HistoricalRevisionWriteDisposition disposition = await AppendWithReconciliationAsync(
            token => this.factRepository.AppendRevisionAsync(fact, reviewEvent, token),
            cancellationToken,
            "The canonical historical fact revision could not be reconciled after an ambiguous write.");
        if (disposition == HistoricalRevisionWriteDisposition.Conflict)
        {
            throw new InvalidOperationException("A canonical historical fact identity collided.");
        }
    }

    private async Task AppendSourceAsync(
        HistoricalSourceReference source,
        HistoricalReviewEventType eventType,
        string note,
        CancellationToken cancellationToken)
    {
        HistoricalReviewEvent reviewEvent = CreateReviewEvent(
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            eventType,
            note,
            source.RecordedAtUtc);
        HistoricalRevisionWriteDisposition disposition = await AppendWithReconciliationAsync(
            token => this.sourceRepository.AppendRevisionAsync(source, reviewEvent, token),
            cancellationToken,
            "The canonical historical source revision could not be reconciled after an ambiguous write.");
        if (disposition == HistoricalRevisionWriteDisposition.Conflict)
        {
            throw new InvalidOperationException("A canonical historical source identity collided.");
        }
    }

    private static async Task<HistoricalRevisionWriteDisposition> AppendWithReconciliationAsync(
        Func<CancellationToken, Task<HistoricalRevisionWriteDisposition>> appendRevisionAsync,
        CancellationToken cancellationToken,
        string failureMessage)
    {
        ArgumentNullException.ThrowIfNull(appendRevisionAsync);
        List<Exception> failures = new List<Exception>();
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            CancellationToken effectiveToken = attempt == 0
                ? cancellationToken
                : CancellationToken.None;
            try
            {
                return await appendRevisionAsync(effectiveToken);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        throw new AggregateException(failureMessage, failures);
    }

    private static HistoricalReviewEvent CreateReviewEvent(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int revision,
        HistoricalReviewEventType eventType,
        string note,
        DateTime occurredAtUtc)
    {
        string resourceName = resourceType.ToString().ToLowerInvariant();
        return new HistoricalReviewEvent(
            HistoricalNarrativeCanonicalIdentity.CreateGuid(
                HistoricalNarrativeCanonicalizationPolicy.Version,
                $"{resourceName}-review-{revision.ToString(CultureInfo.InvariantCulture)}",
                resourceId.ToString("N", CultureInfo.InvariantCulture),
                occurredAtUtc),
            resourceType,
            resourceId,
            revision,
            eventType,
            HistoricalNarrativeCanonicalizationPolicy.Actor,
            note,
            occurredAtUtc);
    }
}
