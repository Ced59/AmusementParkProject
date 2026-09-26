namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

internal static class HistoricalLegacyMigrationAnomalyCodes
{
    internal const string MissingSubject = "missing-subject";
    internal const string HiddenSubject = "hidden-subject";
    internal const string NotRelevantSubject = "not-relevant-subject";
    internal const string UnknownEventType = "unknown-event-type";
    internal const string InvalidDate = "invalid-date";
    internal const string IncompleteSource = "incomplete-source";
    internal const string InvalidSource = "invalid-source";
    internal const string MissingStructuredValue = "missing-structured-value";
    internal const string UnconvertedAssociations = "unconverted-associations";
    internal const string ConversionFailed = "conversion-failed";
    internal const string NarrativeUpdatedAfterMigration = "narrative-updated-after-migration";
}
