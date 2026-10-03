namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Révision immuable d'un lien historique explicitement documenté entre deux sujets.
/// </summary>
public sealed class HistoricalRelation
{
    private const int MaximumSourceReferenceCount = 100;

    public HistoricalRelation(
        Guid id,
        HistoricalSubject source,
        HistoricalSubject target,
        HistoricalRelationType type,
        HistoricalRelationDirection direction,
        HistoricalPeriod period,
        HistoricalFactState state,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        IReadOnlyCollection<HistoricalLocalizedText> publicUncertaintyExplanation,
        IReadOnlyCollection<HistoricalRelationSourceRevisionReference> sourceReferences,
        string? editorialNote,
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
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidIdentifier, "A historical relation requires an identifier.");
        }

        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(period);
        HistoricalRelationTypeValidator.Validate(source, target, type, direction);
        ValidateEnums(state, workflowState, publicationState, revisionOrigin);
        ValidateRevision(revision, supersedesRevision, workflowState, publicationState);
        EnsureUtc(recordedAtUtc);
        EnsureOptionalUtc(verifiedAtUtc);
        EnsureOptionalUtc(publishedAtUtc);
        if (verifiedAtUtc > recordedAtUtc
            || publishedAtUtc > recordedAtUtc
            || verifiedAtUtc.HasValue && publishedAtUtc.HasValue && verifiedAtUtc > publishedAtUtc)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidTimestamp, "Historical relation timestamps are inconsistent.");
        }

        HistoricalRelationSourceRevisionReference[] normalizedSources = sourceReferences?
            .Distinct()
            .OrderBy(static reference => reference.SourceId)
            .ThenBy(static reference => reference.Revision)
            .ToArray()
            ?? throw new ArgumentNullException(nameof(sourceReferences));
        if (normalizedSources.Length > MaximumSourceReferenceCount)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidSourceReference, "A relation cites too many sources.");
        }

        HistoricalLocalizedText[] normalizedExplanations = publicUncertaintyExplanation?
            .DistinctBy(static explanation => explanation.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static explanation => explanation.LanguageCode, StringComparer.Ordinal)
            .ToArray()
            ?? throw new ArgumentNullException(nameof(publicUncertaintyExplanation));
        string? normalizedEditorialNote = NormalizeOptional(editorialNote, 4000);
        string? normalizedMethodology = NormalizeOptional(publicationMethodologyVersion, 100);
        ValidateLifecycle(
            source,
            target,
            state,
            workflowState,
            publicationState,
            normalizedSources,
            normalizedExplanations,
            verifiedAtUtc,
            publishedAtUtc,
            normalizedMethodology);

        this.Id = id;
        this.Source = source;
        this.Target = target;
        this.Type = type;
        this.Direction = direction;
        this.Period = period;
        this.State = state;
        this.WorkflowState = workflowState;
        this.PublicationState = publicationState;
        this.PublicUncertaintyExplanation = Array.AsReadOnly(normalizedExplanations);
        this.SourceReferences = Array.AsReadOnly(normalizedSources);
        this.EditorialNote = normalizedEditorialNote;
        this.VerifiedAtUtc = verifiedAtUtc;
        this.PublishedAtUtc = publishedAtUtc;
        this.PublicationMethodologyVersion = normalizedMethodology;
        this.Revision = revision;
        this.SupersedesRevision = supersedesRevision;
        this.RecordedAtUtc = recordedAtUtc;
        this.RevisionOrigin = revisionOrigin;
    }

    public Guid Id { get; }

    public HistoricalSubject Source { get; }

    public HistoricalSubject Target { get; }

    public HistoricalRelationType Type { get; }

    public HistoricalRelationDirection Direction { get; }

    public HistoricalPeriod Period { get; }

    public HistoricalFactState State { get; }

    public HistoricalEditorialWorkflowState WorkflowState { get; }

    public HistoricalPublicationState PublicationState { get; }

    public IReadOnlyList<HistoricalLocalizedText> PublicUncertaintyExplanation { get; }

    public IReadOnlyList<HistoricalRelationSourceRevisionReference> SourceReferences { get; }

    public string? EditorialNote { get; }

    public DateTime? VerifiedAtUtc { get; }

    public DateTime? PublishedAtUtc { get; }

    public string? PublicationMethodologyVersion { get; }

    public int Revision { get; }

    public int? SupersedesRevision { get; }

    public DateTime RecordedAtUtc { get; }

    public HistoricalRevisionOrigin RevisionOrigin { get; }

    public bool IsDecisionEligible => this.PublicationState == HistoricalPublicationState.Published
        && this.State is HistoricalFactState.Verified or HistoricalFactState.Probable or HistoricalFactState.Disputed;

    public HistoricalRelation CreateRetraction(DateTime recordedAtUtc)
    {
        if (this.PublicationState == HistoricalPublicationState.Withdrawn)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidRelation, "A withdrawn relation cannot be retracted again.");
        }

        return new HistoricalRelation(
            this.Id,
            this.Source,
            this.Target,
            this.Type,
            this.Direction,
            this.Period,
            HistoricalFactState.Retracted,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            Array.Empty<HistoricalLocalizedText>(),
            this.SourceReferences,
            this.EditorialNote,
            null,
            null,
            this.PublicationMethodologyVersion,
            this.Revision + 1,
            this.Revision,
            recordedAtUtc,
            this.RevisionOrigin);
    }

    private static void ValidateEnums(
        HistoricalFactState state,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalRevisionOrigin revisionOrigin)
    {
        if (!Enum.IsDefined(state)
            || !Enum.IsDefined(workflowState)
            || !Enum.IsDefined(publicationState)
            || !Enum.IsDefined(revisionOrigin))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidEnum, "A historical relation contains an invalid state.");
        }
    }

    private static void ValidateRevision(
        int revision,
        int? supersedesRevision,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState)
    {
        bool chainIsValid = revision >= 1
            && (revision == 1 ? !supersedesRevision.HasValue : supersedesRevision == revision - 1);
        bool initialRevisionIsValid = revision != 1
            || workflowState == HistoricalEditorialWorkflowState.Draft
                && publicationState == HistoricalPublicationState.Draft;
        if (!chainIsValid || !initialRevisionIsValid)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidRevision, "A relation revision must form an ordinary immutable chain.");
        }
    }

    private static void ValidateLifecycle(
        HistoricalSubject source,
        HistoricalSubject target,
        HistoricalFactState state,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        IReadOnlyCollection<HistoricalRelationSourceRevisionReference> sources,
        IReadOnlyCollection<HistoricalLocalizedText> explanations,
        DateTime? verifiedAtUtc,
        DateTime? publishedAtUtc,
        string? methodology)
    {
        bool retractionIsValid = state == HistoricalFactState.Retracted
            ? workflowState == HistoricalEditorialWorkflowState.Retracted
                && publicationState == HistoricalPublicationState.Withdrawn
            : workflowState != HistoricalEditorialWorkflowState.Retracted
                && publicationState != HistoricalPublicationState.Withdrawn;
        bool verificationIsValid = (state == HistoricalFactState.Verified) == verifiedAtUtc.HasValue;
        bool publicationIsValid = publicationState switch
        {
            HistoricalPublicationState.Draft => workflowState < HistoricalEditorialWorkflowState.Published
                && !publishedAtUtc.HasValue
                && methodology is null,
            HistoricalPublicationState.Published => workflowState is HistoricalEditorialWorkflowState.Published
                    or HistoricalEditorialWorkflowState.Corrected
                && state is HistoricalFactState.Verified or HistoricalFactState.Probable or HistoricalFactState.Disputed
                && sources.Count > 0
                && publishedAtUtc.HasValue
                && methodology is not null,
            HistoricalPublicationState.Withdrawn => workflowState == HistoricalEditorialWorkflowState.Retracted
                && state == HistoricalFactState.Retracted
                && methodology is not null,
            _ => false,
        };
        bool subjectsCanBePublic = source.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed
            && target.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed;
        bool reviewNeedsSources = workflowState >= HistoricalEditorialWorkflowState.SourcesAttached
            && workflowState != HistoricalEditorialWorkflowState.Retracted;
        if (!retractionIsValid
            || !verificationIsValid
            || !publicationIsValid
            || publicationState == HistoricalPublicationState.Published && !subjectsCanBePublic
            || reviewNeedsSources && sources.Count == 0
            || state == HistoricalFactState.Verified
                && workflowState < HistoricalEditorialWorkflowState.StructuredValidation)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidRelation, "The relation proof, workflow and publication states are inconsistent.");
        }

        if (publicationState == HistoricalPublicationState.Published
            && state is HistoricalFactState.Probable or HistoricalFactState.Disputed)
        {
            bool coversEveryLanguage = HistoricalLocalizationPolicy.SupportedLanguageCodes.All(language =>
                explanations.Any(explanation => string.Equals(
                    explanation.LanguageCode,
                    language,
                    StringComparison.OrdinalIgnoreCase)));
            if (!coversEveryLanguage)
            {
                throw Invalid(
                    HistoricalPersistenceErrorCodes.MissingUncertaintyExplanation,
                    "A public uncertain relation requires an explanation in every supported language.");
            }
        }
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is not null && (normalized.Length > maximumLength || normalized.Any(char.IsControl)))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "A historical relation text field is invalid.");
        }

        return normalized;
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidTimestamp, "Historical relation timestamps must be UTC.");
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
