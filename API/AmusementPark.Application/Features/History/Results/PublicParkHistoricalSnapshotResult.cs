using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Results;

public sealed record PublicParkHistoricalSnapshotResult(
    Park Park,
    ParkHistoricalSnapshot Snapshot,
    IReadOnlyCollection<HistoricalFact> Facts,
    IReadOnlyDictionary<string, string> ZoneNames);
