namespace AmusementPark.Core.Domain.FactualEvents;

public static class FactualEventErrorCodes
{
    public const string InvalidState = "watch.event.invalid-state";
    public const string InvalidIdentifier = "watch.event.invalid-identifier";
    public const string InvalidType = "watch.event.invalid-type";
    public const string InvalidTarget = "watch.event.invalid-target";
    public const string IncompatibleTarget = "watch.event.incompatible-target";
    public const string InvalidFactValue = "watch.event.invalid-fact-value";
    public const string UnchangedFact = "watch.event.unchanged-fact";
    public const string InvalidSource = "watch.event.invalid-source";
    public const string InvalidConfidence = "watch.event.invalid-confidence";
    public const string InsufficientConfidence = "watch.event.insufficient-confidence";
    public const string InvalidDeduplicationKey = "watch.event.invalid-deduplication-key";
    public const string InvalidRevision = "watch.event.invalid-revision";
    public const string InvalidDefinition = "watch.event.invalid-definition";
    public const string InvalidDefinitionVersion = "watch.event.invalid-definition-version";
    public const string InvalidTimestamp = "watch.event.invalid-timestamp";
    public const string InvalidTransition = "watch.event.invalid-transition";
    public const string InvalidVersion = "watch.event.invalid-version";
    public const string MissingSupersedingEvent = "watch.event.missing-superseding-event";
    public const string InvalidReasonCode = "watch.event.invalid-reason-code";
}
