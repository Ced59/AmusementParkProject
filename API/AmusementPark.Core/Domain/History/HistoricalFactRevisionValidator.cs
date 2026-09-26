namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Protège la continuité d'une chaîne de révisions historiques immuables.
/// </summary>
public static class HistoricalFactRevisionValidator
{
    public static void ValidatePredecessor(HistoricalFact fact, HistoricalFact? predecessor)
    {
        ArgumentNullException.ThrowIfNull(fact);
        if (fact.Revision == 1)
        {
            if (predecessor is not null)
            {
                throw Invalid();
            }

            return;
        }

        if (predecessor is null
            || fact.SupersedesRevision != fact.Revision - 1
            || predecessor.Id != fact.Id
            || predecessor.Revision != fact.SupersedesRevision
            || predecessor.RecordedAtUtc > fact.RecordedAtUtc)
        {
            throw Invalid();
        }
    }

    private static HistoricalPersistenceValidationException Invalid()
    {
        return new HistoricalPersistenceValidationException(
            HistoricalPersistenceErrorCodes.InvalidRevision,
            "A historical fact correction must supersede an existing earlier revision of the same fact.");
    }
}
