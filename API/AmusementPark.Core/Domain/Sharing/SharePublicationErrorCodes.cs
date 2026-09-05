namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Codes métier stables associés au cycle de vie d'une publication.
/// </summary>
public static class SharePublicationErrorCodes
{
    public const string InvalidPublicationType = "share-publication.invalid-publication-type";

    public const string InvalidStatus = "share-publication.invalid-status";

    public const string InvalidVisibility = "share-publication.invalid-visibility";

    public const string PolicyTypeMismatch = "share-publication.policy-type-mismatch";

    public const string InvalidSourceVersion = "share-publication.invalid-source-version";

    public const string InvalidPublicationVersion = "share-publication.invalid-publication-version";

    public const string PublicationVersionConflict = "share-publication.publication-version-conflict";

    public const string PublicationVersionOverflow = "share-publication.publication-version-overflow";

    public const string PreviewSourceVersionMismatch = "share-publication.preview-source-version-mismatch";

    public const string PreviewPolicyMismatch = "share-publication.preview-policy-mismatch";

    public const string InvalidTransition = "share-publication.invalid-transition";

    public const string PublicVisibilityRequired = "share-publication.public-visibility-required";

    public const string ShareTokenRequired = "share-publication.share-token-required";

    public const string ShareTokenUnchanged = "share-publication.share-token-unchanged";

    public const string TimestampNotUtc = "share-publication.timestamp-not-utc";

    public const string InvalidTimestampOrder = "share-publication.invalid-timestamp-order";

    public const string InvalidRestoredState = "share-publication.invalid-restored-state";
}
