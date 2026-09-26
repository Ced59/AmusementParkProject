namespace AmusementPark.Core.Domain.History;

public sealed class HistoricalSourceReference
{
    public HistoricalSourceReference(
        Guid id,
        int revision,
        HistoricalSourceType type,
        string title,
        string publisherOrAuthor,
        string? url,
        string? bibliographicReference,
        DateOnly? publishedOn,
        DateOnly accessedOn,
        string? languageCode,
        string? archiveUrl,
        IReadOnlyCollection<HistoricalSourceScope> scopes,
        string? adminNote,
        HistoricalSourceAccessibility accessibility,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        DateTime recordedAtUtc,
        HistoricalRevisionOrigin revisionOrigin = HistoricalRevisionOrigin.Ordinary)
    {
        if (id == Guid.Empty)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidIdentifier, "A historical source requires an identifier.");
        }

        if (revision < 1)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidRevision, "A historical source revision must be positive.");
        }

        ValidateEnums(type, accessibility, workflowState, publicationState, revisionOrigin);
        ValidateInitialRevision(revision, workflowState, publicationState, revisionOrigin);
        string normalizedTitle = NormalizeRequired(title, 500, nameof(title));
        string normalizedPublisher = NormalizeRequired(publisherOrAuthor, 300, nameof(publisherOrAuthor));
        string? normalizedUrl = NormalizeOptionalUri(url, nameof(url));
        string? normalizedReference = NormalizeOptional(bibliographicReference, 1000);
        if (normalizedUrl is null && normalizedReference is null)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceReference,
                "A historical source requires a stable URL or bibliographic reference.");
        }

        if (publishedOn.HasValue && publishedOn.Value > accessedOn)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidTimestamp,
                "A historical source cannot be accessed before it was published.");
        }

        EnsureUtc(recordedAtUtc);
        if (accessedOn > DateOnly.FromDateTime(recordedAtUtc))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidTimestamp,
                "A historical source access date cannot follow its recorded revision.");
        }

        string? normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        string? normalizedArchiveUrl = NormalizeOptionalUri(archiveUrl, nameof(archiveUrl));
        HistoricalSourceScope[] normalizedScopes = NormalizeScopes(scopes);
        ValidatePublication(workflowState, publicationState, accessibility, revisionOrigin);

        this.Id = id;
        this.Revision = revision;
        this.Type = type;
        this.Title = normalizedTitle;
        this.PublisherOrAuthor = normalizedPublisher;
        this.Url = normalizedUrl;
        this.BibliographicReference = normalizedReference;
        this.PublishedOn = publishedOn;
        this.AccessedOn = accessedOn;
        this.LanguageCode = normalizedLanguageCode;
        this.ArchiveUrl = normalizedArchiveUrl;
        this.Scopes = Array.AsReadOnly(normalizedScopes);
        this.AdminNote = NormalizeOptional(adminNote, 4000);
        this.Accessibility = accessibility;
        this.WorkflowState = workflowState;
        this.PublicationState = publicationState;
        this.RecordedAtUtc = recordedAtUtc;
        this.RevisionOrigin = revisionOrigin;
    }

    public Guid Id { get; }

    public int Revision { get; }

    public HistoricalSourceType Type { get; }

    public string Title { get; }

    public string PublisherOrAuthor { get; }

    public string? Url { get; }

    public string? BibliographicReference { get; }

    public DateOnly? PublishedOn { get; }

    public DateOnly AccessedOn { get; }

    public string? LanguageCode { get; }

    public string? ArchiveUrl { get; }

    public IReadOnlyList<HistoricalSourceScope> Scopes { get; }

    public string? AdminNote { get; }

    public HistoricalSourceAccessibility Accessibility { get; }

    public HistoricalEditorialWorkflowState WorkflowState { get; }

    public HistoricalPublicationState PublicationState { get; }

    public DateTime RecordedAtUtc { get; }

    public HistoricalRevisionOrigin RevisionOrigin { get; }

    private static void ValidateEnums(
        HistoricalSourceType type,
        HistoricalSourceAccessibility accessibility,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalRevisionOrigin revisionOrigin)
    {
        if (!Enum.IsDefined(type)
            || !Enum.IsDefined(accessibility)
            || !Enum.IsDefined(workflowState)
            || !Enum.IsDefined(publicationState)
            || !Enum.IsDefined(revisionOrigin))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidEnum, "A historical source contains an invalid state.");
        }
    }

    private static void ValidatePublication(
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalSourceAccessibility accessibility,
        HistoricalRevisionOrigin revisionOrigin)
    {
        bool valid = publicationState switch
        {
            HistoricalPublicationState.Draft => workflowState is not HistoricalEditorialWorkflowState.Published
                and not HistoricalEditorialWorkflowState.Corrected
                and not HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Published => (workflowState is HistoricalEditorialWorkflowState.Published
                    or HistoricalEditorialWorkflowState.Corrected)
                && accessibility is not HistoricalSourceAccessibility.Withdrawn,
            HistoricalPublicationState.LegacyPublishedPendingReview => revisionOrigin
                    == HistoricalRevisionOrigin.LegacyMigration
                && (workflowState is HistoricalEditorialWorkflowState.EditorialReview
                    or HistoricalEditorialWorkflowState.StructuredValidation),
            HistoricalPublicationState.Withdrawn => workflowState == HistoricalEditorialWorkflowState.Retracted,
            _ => false,
        };

        if (!valid)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "The historical source workflow and publication states are inconsistent.");
        }
    }

    private static void ValidateInitialRevision(
        int revision,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalRevisionOrigin revisionOrigin)
    {
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
        bool validPublicationForOrigin = revisionOrigin switch
        {
            HistoricalRevisionOrigin.Ordinary => publicationState
                != HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalRevisionOrigin.LegacyMigration => publicationState
                != HistoricalPublicationState.Draft,
            _ => false,
        };
        if (!validInitialRevision || !validPublicationForOrigin)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidRevision,
                "A historical source must start as a draft or through the explicit legacy migration state.");
        }
    }

    private static HistoricalSourceScope[] NormalizeScopes(IReadOnlyCollection<HistoricalSourceScope> scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        if (scopes.Count == 0 || scopes.Any(static scope => !Enum.IsDefined(scope)))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "A historical source requires at least one valid evidence scope.");
        }

        return scopes.Distinct().OrderBy(static scope => scope).ToArray();
    }

    private static string? NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        bool isValid = normalizedLanguageCode.Length is >= 2 and <= 12
            && normalizedLanguageCode.All(static character => char.IsLetter(character) || character == '-');
        if (!isValid)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "The historical source language code is invalid.");
        }

        return normalizedLanguageCode;
    }

    private static string NormalizeRequired(string value, int maximumLength, string parameterName)
    {
        return NormalizeOptional(value, maximumLength)
            ?? throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "A historical source field is required.", parameterName);
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalizedValue is not null
            && (normalizedValue.Length > maximumLength || normalizedValue.Any(char.IsControl)))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "A historical source field is invalid.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalUri(string? value, string parameterName)
    {
        string? normalizedValue = NormalizeOptional(value, 2000);
        if (normalizedValue is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceReference,
                "A historical source URL must be an absolute HTTP or HTTPS URL.",
                parameterName);
        }

        return uri.AbsoluteUri;
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidTimestamp, "Historical persistence timestamps must be UTC.");
        }
    }

    private static HistoricalPersistenceValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new HistoricalPersistenceValidationException(code, message, parameterName);
    }
}
