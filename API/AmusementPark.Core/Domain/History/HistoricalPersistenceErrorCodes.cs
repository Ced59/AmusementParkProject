namespace AmusementPark.Core.Domain.History;

public static class HistoricalPersistenceErrorCodes
{
    public const string InvalidIdentifier = "history.persistence.invalid_identifier";
    public const string InvalidEnum = "history.persistence.invalid_enum";
    public const string InvalidText = "history.persistence.invalid_text";
    public const string InvalidRevision = "history.persistence.invalid_revision";
    public const string InvalidTimestamp = "history.persistence.invalid_timestamp";
    public const string InvalidFactState = "history.persistence.invalid_fact_state";
    public const string MissingSource = "history.persistence.missing_source";
    public const string MissingUncertaintyExplanation = "history.persistence.missing_uncertainty_explanation";
    public const string InvalidSequence = "history.persistence.invalid_sequence";
    public const string InvalidOtherType = "history.persistence.invalid_other_type";
    public const string InvalidSourceReference = "history.persistence.invalid_source_reference";
    public const string InvalidSourceScope = "history.persistence.invalid_source_scope";
    public const string InvalidReviewEvent = "history.persistence.invalid_review_event";
}
