namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Garantit un ordre d'audit cohérent avec la chaîne de révisions immuables.
/// </summary>
public static class HistoricalReviewEventChronologyValidator
{
    public static void Validate(
        HistoricalReviewEvent reviewEvent,
        HistoricalReviewEvent? predecessorReviewEvent)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);
        bool isValid = reviewEvent.ResourceRevision == 1
            ? predecessorReviewEvent is null
            : predecessorReviewEvent is not null
                && predecessorReviewEvent.ResourceType == reviewEvent.ResourceType
                && predecessorReviewEvent.ResourceId == reviewEvent.ResourceId
                && predecessorReviewEvent.ResourceRevision == reviewEvent.ResourceRevision - 1
                && predecessorReviewEvent.OccurredAtUtc <= reviewEvent.OccurredAtUtc;
        if (!isValid)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidReviewEvent,
                "A historical review event must follow the audit event of the preceding revision.");
        }
    }
}
