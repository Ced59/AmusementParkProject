namespace AmusementPark.Core.Domain.History;

public enum HistoricalPublicationState
{
    Draft = 0,
    Published = 1,
    LegacyPublishedPendingReview = 2,
    Withdrawn = 3,
    Suppressed = 4,
}
