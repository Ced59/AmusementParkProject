using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationService : IHistoricalNarrativeCanonicalizer
{
    public const string CanonicalizationVersion = "hist-canonical-v2";

    private const string Actor = "system:hist-canonicalization";
    private const string MethodologyVersion = "hist-v2-canonical";

    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IStandaloneAttractionRepository? standaloneAttractionRepository;

    public HistoricalNarrativeCanonicalizationService(
        IHistoricalFactRepository factRepository,
        IHistoricalSourceRepository sourceRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IStandaloneAttractionRepository? standaloneAttractionRepository = null)
    {
        this.factRepository = factRepository ?? throw new ArgumentNullException(nameof(factRepository));
        this.sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
        this.parkItemRepository = parkItemRepository ?? throw new ArgumentNullException(nameof(parkItemRepository));
        this.standaloneAttractionRepository = standaloneAttractionRepository;
    }

    public Task<HistoricalNarrativeCanonicalizationResult> CanonicalizeAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        return this.CanonicalizeAsync(historyEvent, preserveRequestedPublication: true, cancellationToken);
    }

    public Task<HistoricalNarrativeCanonicalizationResult> MigrateExistingAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        return this.CanonicalizeAsync(historyEvent, preserveRequestedPublication: true, cancellationToken);
    }

    private async Task<HistoricalNarrativeCanonicalizationResult> CanonicalizeAsync(
        HistoryEvent historyEvent,
        bool preserveRequestedPublication,
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

        HistoricalSubjectResolution? subjectResolution = await this.ResolveSubjectAsync(
            historyEvent,
            cancellationToken);
        if (subjectResolution is null)
        {
            return Blocked("history-canonicalization.missing-subject");
        }

        HistoricalPeriod period;
        try
        {
            period = HistoricalPeriod.Point(BuildDate(historyEvent));
        }
        catch (HistoricalTemporalValidationException)
        {
            return Blocked("history-canonicalization.invalid-date");
        }

        string? structuredValue = BuildStructuredValue(historyEvent, mapping);
        if (mapping.AttributeKind.HasValue && structuredValue is null)
        {
            return Blocked("history-canonicalization.missing-structured-value");
        }

        string? otherTypeLabel = mapping.FactType == HistoricalFactType.Other
            ? historyEvent.EventType
            : null;
        DateTime recordedAtUtc = NormalizeUtc(historyEvent.UpdatedAtUtc > historyEvent.CreatedAtUtc
            ? historyEvent.UpdatedAtUtc
            : historyEvent.CreatedAtUtc);
        HistoricalSubject subject = new HistoricalSubject(
            subjectResolution.SubjectType,
            subjectResolution.SubjectId,
            ResolveHistoricalLabel(historyEvent, subjectResolution.Label),
            subjectResolution.PublicationPolicy,
            subjectResolution.ContextParkId);
        bool publish = preserveRequestedPublication
            && historyEvent.IsVisible
            && subject.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed;
        HistoricalSourcePlan[] sourcePlans = BuildSourcePlans(
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
            sourcePlans = BuildSourcePlans(
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
            await this.PersistSourceAsync(sourcePlan, cancellationToken);
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
        HistoricalFact draft = new HistoricalFact(
            factId,
            subject,
            mapping.FactType,
            period,
            publish ? HistoricalFactState.Probable : HistoricalFactState.Unverified,
            historyEvent.IsMajor ? HistoricalImportance.Major : HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            publish ? BuildProbabilityExplanations() : Array.Empty<HistoricalLocalizedText>(),
            mapping.LifecycleBoundaryMeaning,
            mapping.AttributeKind,
            mapping.AttributeBoundaryMeaning,
            null,
            sourceReferences,
            structuredValue,
            otherTypeLabel,
            historyEvent.Id,
            null,
            null,
            null,
            1,
            null,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
        await this.AppendFactAsync(
            draft,
            HistoricalReviewEventType.Created,
            "Création du fait HIST canonique depuis le contenu éditorial.",
            cancellationToken);
        HistoricalFact latest = draft;
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
                latest = CreateFactRevision(latest, stage, recordedAtUtc);
                await this.AppendFactAsync(
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

    private async Task<HistoricalSubjectResolution?> ResolveSubjectAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        if (historyEvent.EntityType == HistoryEntityType.Park)
        {
            Park? park = await this.parkRepository.GetByIdAsync(
                historyEvent.OwnerId,
                true,
                cancellationToken);
            return park is null
                ? null
                : new HistoricalSubjectResolution(
                    HistoricalSubjectType.Park,
                    park.Id,
                    park.Name ?? historyEvent.OwnerId,
                    park.IsPubliclyDiscoverable()
                        ? HistoricalSubjectPublicationPolicy.FollowCurrentSubject
                        : HistoricalSubjectPublicationPolicy.Suppressed,
                    park.Id);
        }

        if (historyEvent.EntityType == HistoryEntityType.ParkItem)
        {
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(
                historyEvent.OwnerId,
                true,
                cancellationToken);
            if (item is null)
            {
                return null;
            }

            Park? park = await this.parkRepository.GetByIdAsync(
                item.ParkId,
                true,
                cancellationToken);
            bool isPublic = item.IsVisible
                && item.AdminReviewStatus != AdminReviewStatus.NotRelevant
                && park?.IsPubliclyDiscoverable() == true;
            return new HistoricalSubjectResolution(
                HistoricalSubjectType.ParkItem,
                item.Id,
                item.Name,
                isPublic
                    ? HistoricalSubjectPublicationPolicy.FollowCurrentSubject
                    : HistoricalSubjectPublicationPolicy.Suppressed,
                item.ParkId);
        }

        if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction
            && this.standaloneAttractionRepository is not null)
        {
            StandaloneAttraction? attraction = await this.standaloneAttractionRepository.GetByIdAsync(
                historyEvent.OwnerId,
                true,
                cancellationToken);
            return attraction is null
                ? null
                : new HistoricalSubjectResolution(
                    HistoricalSubjectType.StandaloneAttraction,
                    attraction.Id,
                    attraction.Name,
                    attraction.IsPubliclyPublishable()
                        ? HistoricalSubjectPublicationPolicy.HistoricalOnly
                        : HistoricalSubjectPublicationPolicy.Suppressed,
                    null);
        }

        return null;
    }

    private static HistoricalSourcePlan[] BuildSourcePlans(
        HistoryEvent historyEvent,
        HistoricalSubject subject,
        LegacyHistoryEventTypeMapping mapping,
        HistoricalPeriod period,
        string? structuredValue,
        string? otherTypeLabel,
        DateTime recordedAtUtc,
        bool publish,
        List<string> warnings)
    {
        List<HistoricalSourcePlan> plans = new List<HistoricalSourcePlan>();
        for (int index = 0; index < historyEvent.Sources.Count; index++)
        {
            HistorySourceReference source = historyEvent.Sources[index];
            string normalizedUrl = source.Url?.Trim() ?? string.Empty;
            if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uri)
                || uri.Scheme is not ("http" or "https"))
            {
                warnings.Add("history-canonicalization.invalid-source");
                continue;
            }

            DateOnly recordedOn = DateOnly.FromDateTime(recordedAtUtc);
            bool hasValidAccessDate = DateOnly.TryParse(
                    source.AccessedAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly parsedAccessedOn)
                && parsedAccessedOn <= recordedOn;
            if (!hasValidAccessDate)
            {
                warnings.Add("history-canonicalization.source-access-date-normalized");
            }

            HistoricalSourceScope[] scopes = BuildSourceScopes(structuredValue);
            Guid sourceId = HistoricalNarrativeCanonicalIdentity.CreateGuid(
                CanonicalizationVersion,
                "source",
                historyEvent.Id,
                historyEvent.UpdatedAtUtc,
                index);
            HistoricalSourceReference draft = new HistoricalSourceReference(
                sourceId,
                1,
                HistoricalSourceType.Other,
                NormalizeSourceTitle(source.Label, uri.Host),
                uri.Host,
                normalizedUrl,
                null,
                null,
                hasValidAccessDate ? parsedAccessedOn : recordedOn,
                null,
                null,
                scopes,
                null,
                HistoricalSourceAccessibility.Accessible,
                HistoricalEditorialWorkflowState.Draft,
                HistoricalPublicationState.Draft,
                recordedAtUtc,
                HistoricalRevisionOrigin.Ordinary);
            HistoricalSourceReference published = publish
                ? CreateSourceRevision(
                    CreateSourceRevision(
                        CreateSourceRevision(
                            draft,
                            HistoricalEditorialWorkflowState.EditorialReview,
                            recordedAtUtc),
                        HistoricalEditorialWorkflowState.StructuredValidation,
                        recordedAtUtc),
                    HistoricalEditorialWorkflowState.Published,
                    recordedAtUtc)
                : draft;
            plans.Add(new HistoricalSourcePlan(
                draft,
                published,
                subject,
                mapping,
                period,
                structuredValue,
                otherTypeLabel,
                historyEvent.Id));
        }

        return plans.ToArray();
    }

    private async Task PersistSourceAsync(
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
            latest = CreateSourceRevision(latest, target, latest.RecordedAtUtc);
            await this.AppendSourceAsync(
                latest,
                HistoricalEditorialTransitionPolicy.GetAdvanceEventType(target),
                "Conservation de la publication éditoriale existante pendant la migration canonique.",
                cancellationToken);
        }
    }

    private async Task AppendSourceAsync(
        HistoricalSourceReference source,
        HistoricalReviewEventType eventType,
        string note,
        CancellationToken cancellationToken)
    {
        HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
            HistoricalNarrativeCanonicalIdentity.CreateGuid(
                CanonicalizationVersion,
                $"source-review-{source.Revision.ToString(CultureInfo.InvariantCulture)}",
                source.Id.ToString("N", CultureInfo.InvariantCulture),
                source.RecordedAtUtc),
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            eventType,
            Actor,
            note,
            source.RecordedAtUtc);
        HistoricalRevisionWriteDisposition disposition = await this.sourceRepository.AppendRevisionAsync(
            source,
            reviewEvent,
            cancellationToken);
        if (disposition == HistoricalRevisionWriteDisposition.Conflict)
        {
            throw new InvalidOperationException("A canonical historical source identity collided.");
        }
    }

    private async Task AppendFactAsync(
        HistoricalFact fact,
        HistoricalReviewEventType eventType,
        string note,
        CancellationToken cancellationToken)
    {
        HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
            HistoricalNarrativeCanonicalIdentity.CreateGuid(
                CanonicalizationVersion,
                $"fact-review-{fact.Revision.ToString(CultureInfo.InvariantCulture)}",
                fact.Id.ToString("N", CultureInfo.InvariantCulture),
                fact.RecordedAtUtc),
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            eventType,
            Actor,
            note,
            fact.RecordedAtUtc);
        HistoricalRevisionWriteDisposition disposition = await this.factRepository.AppendRevisionAsync(
            fact,
            reviewEvent,
            cancellationToken);
        if (disposition == HistoricalRevisionWriteDisposition.Conflict)
        {
            throw new InvalidOperationException("A canonical historical fact identity collided.");
        }
    }

    private static HistoricalSourceReference CreateSourceRevision(
        HistoricalSourceReference previous,
        HistoricalEditorialWorkflowState stage,
        DateTime recordedAtUtc)
    {
        return new HistoricalSourceReference(
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
            stage,
            stage == HistoricalEditorialWorkflowState.Published
                ? HistoricalPublicationState.Published
                : HistoricalPublicationState.Draft,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    private static HistoricalFact CreateFactRevision(
        HistoricalFact previous,
        HistoricalEditorialWorkflowState stage,
        DateTime recordedAtUtc)
    {
        bool isPublished = stage == HistoricalEditorialWorkflowState.Published;
        return new HistoricalFact(
            previous.Id,
            previous.Subject,
            previous.Type,
            previous.Period,
            previous.State,
            previous.Importance,
            stage,
            isPublished ? HistoricalPublicationState.Published : HistoricalPublicationState.Draft,
            previous.PublicUncertaintyExplanation,
            previous.LifecycleBoundaryMeaning,
            previous.AttributeKind,
            previous.AttributeBoundaryMeaning,
            previous.SequenceWithinDate,
            previous.SourceReferences,
            previous.StructuredValue,
            previous.OtherTypeLabel,
            previous.NarrativeContentId,
            null,
            isPublished ? recordedAtUtc : null,
            isPublished ? MethodologyVersion : null,
            previous.Revision + 1,
            previous.Revision,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    private static HistoricalDate BuildDate(HistoryEvent historyEvent)
    {
        return historyEvent.DatePrecision switch
        {
            HistoryDatePrecision.Year => HistoricalDate.ForYear(historyEvent.Year),
            HistoryDatePrecision.Month => HistoricalDate.ForMonth(historyEvent.Year, historyEvent.Month ?? 0),
            HistoryDatePrecision.Day => HistoricalDate.ForDay(
                historyEvent.Year,
                historyEvent.Month ?? 0,
                historyEvent.Day ?? 0),
            _ => throw new HistoricalTemporalValidationException(
                HistoricalTemporalErrorCodes.InvalidPrecision,
                "The historical date precision is invalid.",
                nameof(historyEvent.DatePrecision)),
        };
    }

    private static string? BuildStructuredValue(
        HistoryEvent historyEvent,
        LegacyHistoryEventTypeMapping mapping)
    {
        if (!mapping.AttributeKind.HasValue)
        {
            return null;
        }

        string? requiredNextKey = mapping.AttributeKind.Value switch
        {
            HistoricalAttributeKind.Name or HistoricalAttributeKind.MarketPositioning
                or HistoricalAttributeKind.Theme => "next",
            HistoricalAttributeKind.Logo => "nextImageId",
            HistoricalAttributeKind.Operator => "nextId",
            _ => null,
        };
        Dictionary<string, string?> values = mapping.AttributeKind.Value switch
        {
            HistoricalAttributeKind.Name or HistoricalAttributeKind.MarketPositioning
                or HistoricalAttributeKind.Theme => new Dictionary<string, string?>
                {
                    ["previous"] = historyEvent.PreviousName,
                    ["next"] = historyEvent.NewName,
                },
            HistoricalAttributeKind.Logo => new Dictionary<string, string?>
                {
                    ["previousImageId"] = historyEvent.PreviousLogoImageId,
                    ["nextImageId"] = historyEvent.NewLogoImageId,
                },
            HistoricalAttributeKind.Operator => new Dictionary<string, string?>
                {
                    ["previousId"] = historyEvent.PreviousOperatorId,
                    ["nextId"] = historyEvent.NewOperatorId,
                },
            _ => new Dictionary<string, string?>(),
        };
        Dictionary<string, string> normalized = values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value!.Trim(),
                StringComparer.Ordinal);
        return normalized.Count == 0
            || requiredNextKey is null
            || !normalized.ContainsKey(requiredNextKey)
                ? null
                : JsonSerializer.Serialize(normalized);
    }

    private static HistoricalSourceScope[] BuildSourceScopes(string? structuredValue)
    {
        List<HistoricalSourceScope> scopes = new List<HistoricalSourceScope>
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
            HistoricalSourceScope.Narrative,
        };
        if (structuredValue is not null)
        {
            scopes.Add(HistoricalSourceScope.StructuredValue);
        }

        return scopes.ToArray();
    }

    private static IReadOnlyCollection<HistoricalLocalizedText> BuildProbabilityExplanations()
    {
        return new[]
        {
            new HistoricalLocalizedText("fr", "Cette information est considérée comme probable au vu des sources disponibles."),
            new HistoricalLocalizedText("en", "This information is considered probable based on the available sources."),
            new HistoricalLocalizedText("de", "Diese Information gilt anhand der verfügbaren Quellen als wahrscheinlich."),
            new HistoricalLocalizedText("nl", "Deze informatie wordt op basis van de beschikbare bronnen als waarschijnlijk beschouwd."),
            new HistoricalLocalizedText("it", "Questa informazione è considerata probabile in base alle fonti disponibili."),
            new HistoricalLocalizedText("es", "Esta información se considera probable según las fuentes disponibles."),
            new HistoricalLocalizedText("pl", "Ta informacja jest uznawana za prawdopodobną na podstawie dostępnych źródeł."),
            new HistoricalLocalizedText("pt", "Esta informação é considerada provável com base nas fontes disponíveis."),
        };
    }

    private static string ResolveHistoricalLabel(HistoryEvent historyEvent, string fallback)
    {
        string label = new[]
            {
                historyEvent.NewName,
                historyEvent.PreviousName,
                fallback,
            }
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?
            .Trim() ?? fallback;
        return label.Length <= 300 ? label : label[..300];
    }

    private static string NormalizeSourceTitle(string? value, string fallback)
    {
        string normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : new string(value.Where(static character => !char.IsControl(character)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = fallback;
        }

        return normalized.Length <= 500 ? normalized : normalized[..500];
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private static HistoricalNarrativeCanonicalizationResult Blocked(string warning)
    {
        return new HistoricalNarrativeCanonicalizationResult(
            null,
            HistoricalNarrativeCanonicalizationState.Blocked,
            new[] { warning });
    }

}
