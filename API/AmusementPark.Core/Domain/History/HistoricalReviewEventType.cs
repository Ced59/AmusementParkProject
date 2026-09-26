namespace AmusementPark.Core.Domain.History;

public enum HistoricalReviewEventType
{
    Created = 0,
    SourcesAttached = 1,
    SubmittedForEditorialReview = 2,
    StructuredValidationCompleted = 3,
    Published = 4,
    Corrected = 5,
    Retracted = 6,
    Migrated = 7,
    DraftUpdated = 8,
}
