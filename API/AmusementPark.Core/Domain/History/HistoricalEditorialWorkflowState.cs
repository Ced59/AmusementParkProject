namespace AmusementPark.Core.Domain.History;

public enum HistoricalEditorialWorkflowState
{
    Draft = 0,
    SourcesAttached = 1,
    EditorialReview = 2,
    StructuredValidation = 3,
    Published = 4,
    Corrected = 5,
    Retracted = 6,
}
