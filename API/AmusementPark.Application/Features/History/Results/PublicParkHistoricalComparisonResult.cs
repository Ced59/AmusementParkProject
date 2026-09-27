using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Results;

public sealed record PublicParkHistoricalComparisonResult(
    Park Park,
    ParkHistoricalComparison Comparison,
    IReadOnlyCollection<HistoricalFact> Facts,
    IReadOnlyDictionary<string, string> ZoneNames);
