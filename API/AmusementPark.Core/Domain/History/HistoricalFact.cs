namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Révision immuable d'une assertion historique structurée.
/// </summary>
public sealed class HistoricalFact
{
    private const int MaximumSourceReferenceCount = 100;

    public HistoricalFact(
        Guid id,
        HistoricalSubject subject,
        HistoricalFactType type,
        HistoricalPeriod period,
        HistoricalFactState state,
        HistoricalImportance importance,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        IReadOnlyCollection<HistoricalLocalizedText> publicUncertaintyExplanation,
        LifecycleBoundaryMeaning? lifecycleBoundaryMeaning,
        HistoricalAttributeKind? attributeKind,
        AttributeBoundaryMeaning? attributeBoundaryMeaning,
        int? sequenceWithinDate,
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences,
        string? structuredValue,
        string? otherTypeLabel,
        string? narrativeContentId,
        DateTime? verifiedAtUtc,
        DateTime? publishedAtUtc,
        string? publicationMethodologyVersion,
        int revision,
        int? supersedesRevision,
        DateTime recordedAtUtc,
        HistoricalRevisionOrigin revisionOrigin = HistoricalRevisionOrigin.Ordinary)
    {
        if (id == Guid.Empty)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidIdentifier, "A historical fact requires an identifier.");
        }

        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(period);
        ValidateEnums(type, state, importance, workflowState, publicationState, revisionOrigin);
        ValidateRevision(revision, supersedesRevision, workflowState, publicationState, revisionOrigin);
        EnsureUtc(recordedAtUtc);
        EnsureOptionalUtc(verifiedAtUtc);
        EnsureOptionalUtc(publishedAtUtc);
        if (verifiedAtUtc > recordedAtUtc || publishedAtUtc > recordedAtUtc)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidTimestamp,
                "Historical fact lifecycle timestamps cannot follow their recorded revision.");
        }

        if (verifiedAtUtc.HasValue
            && publishedAtUtc.HasValue
            && verifiedAtUtc.Value > publishedAtUtc.Value)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidTimestamp,
                "A historical fact must be verified before it is published.");
        }

        HistoricalSourceRevisionReference[] normalizedSourceReferences =
            NormalizeSourceReferences(sourceReferences);
        HistoricalLocalizedText[] normalizedExplanations = NormalizeExplanations(publicUncertaintyExplanation);
        string? normalizedStructuredValue = NormalizeOptional(structuredValue, 16000);
        string? normalizedOtherTypeLabel = NormalizeOptional(otherTypeLabel, 200);
        string? normalizedNarrativeContentId = NormalizeOptional(narrativeContentId, 200);
        string? normalizedMethodologyVersion = NormalizeOptional(publicationMethodologyVersion, 100);

        ValidateOtherType(type, normalizedOtherTypeLabel, normalizedSourceReferences);
        ValidateBoundarySemantics(
            type,
            period,
            normalizedStructuredValue,
            lifecycleBoundaryMeaning,
            attributeKind,
            attributeBoundaryMeaning,
            sequenceWithinDate);
        ValidateLifecycle(
            subject,
            state,
            workflowState,
            publicationState,
            normalizedSourceReferences,
            normalizedExplanations,
            verifiedAtUtc,
            publishedAtUtc,
            normalizedMethodologyVersion);

        this.Id = id;
        this.Subject = subject;
        this.Type = type;
        this.Period = period;
        this.State = state;
        this.Importance = importance;
        this.WorkflowState = workflowState;
        this.PublicationState = publicationState;
        this.PublicUncertaintyExplanation = Array.AsReadOnly(normalizedExplanations);
        this.LifecycleBoundaryMeaning = lifecycleBoundaryMeaning;
        this.AttributeKind = attributeKind;
        this.AttributeBoundaryMeaning = attributeBoundaryMeaning;
        this.SequenceWithinDate = sequenceWithinDate;
        this.SourceReferences = Array.AsReadOnly(normalizedSourceReferences);
        this.StructuredValue = normalizedStructuredValue;
        this.OtherTypeLabel = normalizedOtherTypeLabel;
        this.NarrativeContentId = normalizedNarrativeContentId;
        this.VerifiedAtUtc = verifiedAtUtc;
        this.PublishedAtUtc = publishedAtUtc;
        this.PublicationMethodologyVersion = normalizedMethodologyVersion;
        this.Revision = revision;
        this.SupersedesRevision = supersedesRevision;
        this.RecordedAtUtc = recordedAtUtc;
        this.RevisionOrigin = revisionOrigin;
    }

    public Guid Id { get; }

    public HistoricalSubject Subject { get; }

    public HistoricalFactType Type { get; }

    public HistoricalPeriod Period { get; }

    public HistoricalFactState State { get; }

    public HistoricalImportance Importance { get; }

    public HistoricalEditorialWorkflowState WorkflowState { get; }

    public HistoricalPublicationState PublicationState { get; }

    public IReadOnlyList<HistoricalLocalizedText> PublicUncertaintyExplanation { get; }

    public LifecycleBoundaryMeaning? LifecycleBoundaryMeaning { get; }

    public HistoricalAttributeKind? AttributeKind { get; }

    public AttributeBoundaryMeaning? AttributeBoundaryMeaning { get; }

    public int? SequenceWithinDate { get; }

    public IReadOnlyList<HistoricalSourceRevisionReference> SourceReferences { get; }

    public string? StructuredValue { get; }

    public string? OtherTypeLabel { get; }

    public string? NarrativeContentId { get; }

    public DateTime? VerifiedAtUtc { get; }

    public DateTime? PublishedAtUtc { get; }

    public string? PublicationMethodologyVersion { get; }

    public int Revision { get; }

    public int? SupersedesRevision { get; }

    public DateTime RecordedAtUtc { get; }

    public HistoricalRevisionOrigin RevisionOrigin { get; }

    public bool IsDecisionEligible => this.PublicationState == HistoricalPublicationState.Published
        && (this.State is HistoricalFactState.Verified
            or HistoricalFactState.Probable
            or HistoricalFactState.Disputed);

    private static void ValidateEnums(
        HistoricalFactType type,
        HistoricalFactState state,
        HistoricalImportance importance,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalRevisionOrigin revisionOrigin)
    {
        if (!Enum.IsDefined(type)
            || !Enum.IsDefined(state)
            || !Enum.IsDefined(importance)
            || !Enum.IsDefined(workflowState)
            || !Enum.IsDefined(publicationState)
            || !Enum.IsDefined(revisionOrigin))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidEnum, "A historical fact contains an invalid state.");
        }
    }

    private static void ValidateRevision(
        int revision,
        int? supersedesRevision,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalRevisionOrigin revisionOrigin)
    {
        bool valid = revision >= 1
            && (revision == 1
                ? !supersedesRevision.HasValue
                : supersedesRevision == revision - 1);
        if (!valid)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidRevision,
                "A historical fact revision must be positive and identify the previous revision it supersedes.");
        }


        bool validInitialRevision = revisionOrigin switch
        {
            HistoricalRevisionOrigin.Ordinary => revision != 1
                || (workflowState == HistoricalEditorialWorkflowState.Draft
                    && publicationState == HistoricalPublicationState.Draft),
            HistoricalRevisionOrigin.LegacyMigration => revision != 1
                || (workflowState == HistoricalEditorialWorkflowState.EditorialReview
                    && publicationState == HistoricalPublicationState.LegacyPublishedPendingReview),
            _ => false,
        };
        if (!validInitialRevision
            || (revision > 1
                && publicationState == HistoricalPublicationState.LegacyPublishedPendingReview))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidRevision,
                "A historical fact must start as a draft or through the explicit legacy migration state.");
        }
    }

    private static HistoricalSourceRevisionReference[] NormalizeSourceReferences(
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences)
    {
        ArgumentNullException.ThrowIfNull(sourceReferences);
        if (sourceReferences.Count > MaximumSourceReferenceCount)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                $"A historical fact cannot cite more than {MaximumSourceReferenceCount} source revisions.");
        }

        if (sourceReferences.Any(static sourceReference => sourceReference is null))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical fact contains an invalid source revision reference.");
        }

        HistoricalSourceRevisionReference[] normalized = sourceReferences
            .OrderBy(static sourceReference => sourceReference.SourceId)
            .ToArray();
        if (normalized.Select(static sourceReference => sourceReference.SourceId).Distinct().Count()
            != normalized.Length)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidRevision,
                "A historical fact can reference only one immutable revision of each source.");
        }

        return normalized;
    }

    private static HistoricalLocalizedText[] NormalizeExplanations(
        IReadOnlyCollection<HistoricalLocalizedText> explanations)
    {
        ArgumentNullException.ThrowIfNull(explanations);
        if (explanations.Any(static explanation => explanation is null))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "A historical uncertainty explanation is invalid.");
        }

        HistoricalLocalizedText[] normalized = explanations
            .DistinctBy(static explanation => explanation.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static explanation => explanation.LanguageCode, StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length != explanations.Count)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "A historical uncertainty language can only occur once.");
        }

        return normalized;
    }

    private static void ValidateOtherType(
        HistoricalFactType type,
        string? otherTypeLabel,
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences)
    {
        if (type == HistoricalFactType.Other)
        {
            if (otherTypeLabel is null || sourceReferences.Count == 0)
            {
                throw Invalid(
                    HistoricalPersistenceErrorCodes.InvalidOtherType,
                    "An explicit Other historical fact requires a label and a source.");
            }

            return;
        }

        if (otherTypeLabel is not null)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidOtherType,
                "Only an Other historical fact can define a custom type label.");
        }
    }

    private static void ValidateBoundarySemantics(
        HistoricalFactType type,
        HistoricalPeriod period,
        string? structuredValue,
        LifecycleBoundaryMeaning? lifecycleBoundaryMeaning,
        HistoricalAttributeKind? attributeKind,
        AttributeBoundaryMeaning? attributeBoundaryMeaning,
        int? sequenceWithinDate)
    {
        bool isLifecycleTransition = type is HistoricalFactType.Opening
            or HistoricalFactType.Closure
            or HistoricalFactType.Reopening
            or HistoricalFactType.TemporaryClosure
            or HistoricalFactType.DefinitiveClosure;
        if (isLifecycleTransition != lifecycleBoundaryMeaning.HasValue
            || (lifecycleBoundaryMeaning.HasValue && !Enum.IsDefined(lifecycleBoundaryMeaning.Value)))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "Lifecycle boundary meaning must exist only on lifecycle transition facts.");
        }

        if (isLifecycleTransition && !period.IsPoint)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A lifecycle transition must define one historical boundary rather than an interval.");
        }

        if (lifecycleBoundaryMeaning.HasValue
            && !IsLifecycleBoundaryMeaningValid(type, lifecycleBoundaryMeaning.Value))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "Lifecycle boundary meaning is incompatible with the transition type.");
        }

        HistoricalAttributeKind? expectedAttribute = ResolveExpectedAttribute(type);
        bool hasCompleteAttributeBoundary = attributeKind.HasValue && attributeBoundaryMeaning.HasValue;
        if (expectedAttribute.HasValue
            && (!hasCompleteAttributeBoundary
                || attributeKind != expectedAttribute
                || structuredValue is null))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "An attribute transition fact requires its matching attribute boundary semantics.");
        }

        if (!expectedAttribute.HasValue && (attributeKind.HasValue || attributeBoundaryMeaning.HasValue))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "Only an attribute transition fact can define attribute boundary semantics.");
        }

        if (attributeKind.HasValue && !Enum.IsDefined(attributeKind.Value)
            || attributeBoundaryMeaning.HasValue && !Enum.IsDefined(attributeBoundaryMeaning.Value))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidEnum, "A historical attribute boundary is invalid.");
        }

        if (sequenceWithinDate.HasValue)
        {
            bool isExactDayPoint = period.IsPoint
                && period.Start?.Precision == HistoryDatePrecision.Day
                && period.Start.GetEnvelope().IsExactDay;
            if (sequenceWithinDate.Value < 1 || !isExactDayPoint)
            {
                throw Invalid(
                    HistoricalPersistenceErrorCodes.InvalidSequence,
                    "A historical sequence is only valid for an exact-day point fact.");
            }
        }
    }

    private static bool IsLifecycleBoundaryMeaningValid(
        HistoricalFactType type,
        LifecycleBoundaryMeaning boundaryMeaning)
    {
        return type switch
        {
            HistoricalFactType.Opening or HistoricalFactType.Reopening =>
                boundaryMeaning is global::AmusementPark.Core.Domain.History.LifecycleBoundaryMeaning.FirstOperatingDay
                    or global::AmusementPark.Core.Domain.History.LifecycleBoundaryMeaning.Unspecified,
            HistoricalFactType.Closure
                or HistoricalFactType.TemporaryClosure
                or HistoricalFactType.DefinitiveClosure =>
                boundaryMeaning is global::AmusementPark.Core.Domain.History.LifecycleBoundaryMeaning.LastOperatingDay
                    or global::AmusementPark.Core.Domain.History.LifecycleBoundaryMeaning.FirstClosedDay
                    or global::AmusementPark.Core.Domain.History.LifecycleBoundaryMeaning.Unspecified,
            _ => false,
        };
    }

    private static HistoricalAttributeKind? ResolveExpectedAttribute(HistoricalFactType type)
    {
        return type switch
        {
            HistoricalFactType.Renaming => HistoricalAttributeKind.Name,
            HistoricalFactType.ZoneRenaming => HistoricalAttributeKind.Name,
            HistoricalFactType.OperatorChange => HistoricalAttributeKind.Operator,
            HistoricalFactType.OwnerChange => HistoricalAttributeKind.Owner,
            HistoricalFactType.PositioningChange => HistoricalAttributeKind.Location,
            HistoricalFactType.Relocation => HistoricalAttributeKind.Location,
            HistoricalFactType.Retheming => HistoricalAttributeKind.Theme,
            HistoricalFactType.ManufacturerChange => HistoricalAttributeKind.Manufacturer,
            HistoricalFactType.ZoneMove => HistoricalAttributeKind.Zone,
            _ => null,
        };
    }

    private static void ValidateLifecycle(
        HistoricalSubject subject,
        HistoricalFactState state,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences,
        IReadOnlyCollection<HistoricalLocalizedText> explanations,
        DateTime? verifiedAtUtc,
        DateTime? publishedAtUtc,
        string? methodologyVersion)
    {
        bool retractionLifecycleIsConsistent = state == HistoricalFactState.Retracted
            ? workflowState == HistoricalEditorialWorkflowState.Retracted
                && publicationState == HistoricalPublicationState.Withdrawn
            : workflowState != HistoricalEditorialWorkflowState.Retracted
                && publicationState != HistoricalPublicationState.Withdrawn;
        if (!retractionLifecycleIsConsistent)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A retracted historical fact must exclusively use the retracted and withdrawn lifecycle.");
        }

        if ((state == HistoricalFactState.Verified) != verifiedAtUtc.HasValue)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "Only a verified historical fact carries its verification timestamp.");
        }

        if (state == HistoricalFactState.Verified && sourceReferences.Count == 0)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.MissingSource, "A verified historical fact requires evidence.");
        }

        if (state == HistoricalFactState.Verified
            && workflowState < HistoricalEditorialWorkflowState.StructuredValidation)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A historical fact can only be verified after structured validation.");
        }

        bool publicationIsValid = publicationState switch
        {
            HistoricalPublicationState.Draft => workflowState is not HistoricalEditorialWorkflowState.Published
                and not HistoricalEditorialWorkflowState.Corrected
                and not HistoricalEditorialWorkflowState.Retracted
                && !publishedAtUtc.HasValue
                && methodologyVersion is null,
            HistoricalPublicationState.Published => (workflowState is HistoricalEditorialWorkflowState.Published
                    or HistoricalEditorialWorkflowState.Corrected)
                && (state is HistoricalFactState.Verified
                    or HistoricalFactState.Probable
                    or HistoricalFactState.Disputed)
                && sourceReferences.Count > 0
                && publishedAtUtc.HasValue
                && methodologyVersion is not null,
            HistoricalPublicationState.LegacyPublishedPendingReview => workflowState == HistoricalEditorialWorkflowState.EditorialReview
                && state == HistoricalFactState.Unverified
                && methodologyVersion is not null,
            HistoricalPublicationState.Withdrawn => workflowState == HistoricalEditorialWorkflowState.Retracted
                && state == HistoricalFactState.Retracted
                && methodologyVersion is not null,
            _ => false,
        };
        if (!publicationIsValid
            || (subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed
                && publicationState is HistoricalPublicationState.Published
                    or HistoricalPublicationState.LegacyPublishedPendingReview))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "The historical fact proof, workflow and publication states are inconsistent.");
        }

        bool requiresPublicExplanation = (publicationState is HistoricalPublicationState.Published
                or HistoricalPublicationState.LegacyPublishedPendingReview)
            && (state is HistoricalFactState.Probable
                or HistoricalFactState.Disputed
                or HistoricalFactState.Unverified);
        if (requiresPublicExplanation)
        {
            string[] availableLanguages = explanations
                .Select(static explanation => explanation.LanguageCode)
                .ToArray();
            bool coversEveryLanguage = HistoricalLocalizationPolicy.SupportedLanguageCodes
                .All(languageCode => availableLanguages.Contains(languageCode, StringComparer.OrdinalIgnoreCase));
            if (!coversEveryLanguage)
            {
                throw Invalid(
                    HistoricalPersistenceErrorCodes.MissingUncertaintyExplanation,
                    "A public uncertain historical fact requires an explanation in every supported language.");
            }
        }
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalizedValue is not null
            && (normalizedValue.Length > maximumLength || normalizedValue.Any(char.IsControl)))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "A historical fact text field is invalid.");
        }

        return normalizedValue;
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidTimestamp, "Historical persistence timestamps must be UTC.");
        }
    }

    private static void EnsureOptionalUtc(DateTime? value)
    {
        if (value.HasValue)
        {
            EnsureUtc(value.Value);
        }
    }

    private static HistoricalPersistenceValidationException Invalid(string code, string message)
    {
        return new HistoricalPersistenceValidationException(code, message);
    }
}
