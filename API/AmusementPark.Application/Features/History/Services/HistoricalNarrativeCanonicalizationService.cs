using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationService : IHistoricalNarrativeCanonicalizer
{
    public const string CanonicalizationVersion = HistoricalNarrativeCanonicalizationPolicy.Version;

    private readonly HistoricalNarrativeCanonicalSubjectResolver subjectResolver;
    private readonly HistoricalNarrativeCanonicalSourcePlanner sourcePlanner;
    private readonly HistoricalNarrativeCanonicalFactFactory factFactory;
    private readonly HistoricalNarrativeCanonicalRevisionWriter revisionWriter;

    internal HistoricalNarrativeCanonicalizationService(
        HistoricalNarrativeCanonicalSubjectResolver subjectResolver,
        HistoricalNarrativeCanonicalSourcePlanner sourcePlanner,
        HistoricalNarrativeCanonicalFactFactory factFactory,
        HistoricalNarrativeCanonicalRevisionWriter revisionWriter)
    {
        this.subjectResolver = subjectResolver ?? throw new ArgumentNullException(nameof(subjectResolver));
        this.sourcePlanner = sourcePlanner ?? throw new ArgumentNullException(nameof(sourcePlanner));
        this.factFactory = factFactory ?? throw new ArgumentNullException(nameof(factFactory));
        this.revisionWriter = revisionWriter ?? throw new ArgumentNullException(nameof(revisionWriter));
    }

    public Task<HistoricalNarrativeCanonicalizationResult> CanonicalizeAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        return this.CanonicalizeInternalAsync(historyEvent, cancellationToken);
    }

    public Task<HistoricalNarrativeCanonicalizationResult> MigrateExistingAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        return this.CanonicalizeInternalAsync(historyEvent, cancellationToken);
    }

    private async Task<HistoricalNarrativeCanonicalizationResult> CanonicalizeInternalAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        List<string> warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(historyEvent.Id))
        {
            return Blocked("history-canonicalization.missing-narrative-id");
        }

        if (LegacyHistoryEventTypeMapper.RequiresManualClassification(
                historyEvent.EntityType,
                historyEvent.EventType))
        {
            return Blocked("history-canonicalization.manual-classification-required");
        }

        if (!LegacyHistoryEventTypeMapper.TryMap(
                historyEvent.EntityType,
                historyEvent.EventType,
                out LegacyHistoryEventTypeMapping? mapping)
            || mapping is null)
        {
            return Blocked("history-canonicalization.unknown-event-type");
        }

        HistoricalSubjectResolution? subjectResolution = await this.subjectResolver.ResolveAsync(
            historyEvent,
            cancellationToken);
        if (subjectResolution is null)
        {
            return Blocked("history-canonicalization.missing-subject");
        }

        HistoricalPeriod period;
        try
        {
            period = this.factFactory.BuildPeriod(historyEvent);
        }
        catch (HistoricalTemporalValidationException)
        {
            return Blocked("history-canonicalization.invalid-date");
        }

        string? structuredValue = HistoricalNarrativeCanonicalFactFactory.BuildStructuredValue(
            historyEvent,
            mapping);
        if (mapping.AttributeKind.HasValue && structuredValue is null)
        {
            return Blocked("history-canonicalization.missing-structured-value");
        }

        string? otherTypeLabel = mapping.FactType == HistoricalFactType.Other
            ? historyEvent.EventType
            : null;
        DateTime recordedAtUtc = HistoricalNarrativeCanonicalizationPolicy.NormalizeUtc(
            historyEvent.UpdatedAtUtc > historyEvent.CreatedAtUtc
                ? historyEvent.UpdatedAtUtc
                : historyEvent.CreatedAtUtc);
        HistoricalSubject subject = this.factFactory.BuildSubject(historyEvent, subjectResolution);
        bool publish = historyEvent.IsVisible
            && subject.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed;
        HistoricalSourcePlan[] sourcePlans = this.sourcePlanner.BuildPlans(
            historyEvent,
            subject,
            mapping,
            period,
            structuredValue,
            otherTypeLabel,
            recordedAtUtc,
            publish,
            warnings);
        if (mapping.FactType == HistoricalFactType.Other && sourcePlans.Length == 0)
        {
            return Blocked("history-canonicalization.missing-source");
        }

        if (publish && sourcePlans.Length == 0)
        {
            publish = false;
            warnings.Add("history-canonicalization.publication-deferred-without-source");
            sourcePlans = this.sourcePlanner.BuildPlans(
                historyEvent,
                subject,
                mapping,
                period,
                structuredValue,
                otherTypeLabel,
                recordedAtUtc,
                publish,
                warnings);
        }

        foreach (HistoricalSourcePlan sourcePlan in sourcePlans)
        {
            await this.revisionWriter.PersistSourceAsync(sourcePlan, cancellationToken);
        }

        int sourceRevision = publish ? 4 : 1;
        HistoricalSourceRevisionReference[] sourceReferences = sourcePlans
            .Select(plan => plan.CreateReference(sourceRevision))
            .ToArray();
        Guid factId = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            CanonicalizationVersion,
            "fact",
            historyEvent.Id,
            historyEvent.UpdatedAtUtc);
        HistoricalFact latest = this.factFactory.CreateDraft(
            factId,
            historyEvent,
            subject,
            mapping,
            period,
            structuredValue,
            otherTypeLabel,
            sourceReferences,
            publish,
            recordedAtUtc);
        await this.revisionWriter.AppendFactAsync(
            latest,
            HistoricalReviewEventType.Created,
            "Création du fait HIST canonique depuis le contenu éditorial.",
            cancellationToken);
        if (publish)
        {
            foreach (HistoricalEditorialWorkflowState stage in new[]
                     {
                         HistoricalEditorialWorkflowState.SourcesAttached,
                         HistoricalEditorialWorkflowState.EditorialReview,
                         HistoricalEditorialWorkflowState.StructuredValidation,
                         HistoricalEditorialWorkflowState.Published,
                     })
            {
                latest = this.factFactory.CreateRevision(latest, stage, recordedAtUtc);
                await this.revisionWriter.AppendFactAsync(
                    latest,
                    HistoricalEditorialTransitionPolicy.GetAdvanceEventType(stage),
                    "Conservation de la publication éditoriale existante pendant la migration canonique.",
                    cancellationToken);
            }

            HistoricalFactEvidenceValidator.Validate(
                latest,
                sourcePlans.Select(static plan => plan.PublishedSource).ToArray());
        }

        return new HistoricalNarrativeCanonicalizationResult(
            factId,
            HistoricalNarrativeCanonicalizationState.Canonicalized,
            warnings.Distinct(StringComparer.Ordinal).OrderBy(static warning => warning).ToArray());
    }

    private static HistoricalNarrativeCanonicalizationResult Blocked(string warning)
    {
        return new HistoricalNarrativeCanonicalizationResult(
            null,
            HistoricalNarrativeCanonicalizationState.Blocked,
            new[] { warning });
    }
}
