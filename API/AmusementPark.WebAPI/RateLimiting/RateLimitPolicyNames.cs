namespace AmusementPark.WebAPI.RateLimiting;

/// <summary>
/// Policies de rate limiting applicatif appliquées explicitement aux endpoints sensibles.
/// </summary>
public static class RateLimitPolicyNames
{
    public const string AuthLogin = "auth-login";
    public const string AuthExternalLogin = "auth-external-login";
    public const string AuthRefresh = "auth-refresh";
    public const string AuthRegistration = "auth-registration";
    public const string AuthEmailChallenge = "auth-email-challenge";
    public const string AuthPasswordReset = "auth-password-reset";
    public const string ContactSubmission = "contact-submission";
    public const string SocialShareEvents = "social-share-events";
    public const string ImageUploadProcessing = "image-upload-processing";
    public const string ParkDataEditorOperationStatus = "park-data-editor-operation-status";
    public const string RatingDiagnostics = "rating-diagnostics";
    public const string PassportExports = "passport-exports";
    public const string PassportExportDownloads = "passport-export-downloads";
    public const string SharePublicationPreviews = "share-publication-previews";
    public const string SharePublicationConfirmations = "share-publication-confirmations";
    public const string ShareModerationReports = "share-moderation-reports";
    public const string ShareModerationAdministration = "share-moderation-administration";
    public const string ShareSocialImageRendering = "share-social-image-rendering";
    public const string ParkFitSearch = "park-fit-search";
    public const string ParkFitReports = "park-fit-reports";
    public const string ParkFitAdministration = "park-fit-administration";
}
