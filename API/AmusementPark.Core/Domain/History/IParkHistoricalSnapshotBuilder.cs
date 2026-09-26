namespace AmusementPark.Core.Domain.History;

public interface IParkHistoricalSnapshotBuilder
{
    ParkHistoricalSnapshot Build(
        string parkId,
        HistoricalInstant requestedInstant,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> facts);
}
