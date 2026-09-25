namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalSourceRevisionReference
{
    public HistoricalSourceRevisionReference(Guid sourceId, int revision)
    {
        if (sourceId == Guid.Empty)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical source revision reference requires an identifier.");
        }

        if (revision < 1)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidRevision,
                "A historical source revision reference requires a positive revision.");
        }

        this.SourceId = sourceId;
        this.Revision = revision;
    }

    public Guid SourceId { get; }

    public int Revision { get; }
}
