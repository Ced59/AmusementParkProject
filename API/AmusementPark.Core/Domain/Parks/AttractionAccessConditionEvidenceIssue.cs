namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Anomalie empêchant ou fragilisant l'usage décisionnel d'une condition d'accès.
/// </summary>
public enum AttractionAccessConditionEvidenceIssue
{
    UnsupportedSchemaVersion,
    SourceNotDecisionEligible,
    MissingSourceReference,
    InvalidSourceUrl,
    MissingCollectionTimestamp,
    MissingVerificationTimestamp,
    TimestampNotUtc,
    VerificationBeforeCollection,
    VerificationInFuture,
    VerificationStale,
    MissingSourceLanguage,
    InvalidSourceLanguage,
    MissingSourceSummary,
    InsufficientConfidence,
    MissingScopeDetail,
    InvalidScope,
    InvalidEffectivePeriod,
}
