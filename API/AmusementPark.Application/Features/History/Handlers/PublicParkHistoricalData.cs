using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed record PublicParkHistoricalData(
    Park Park,
    IReadOnlyCollection<HistoricalSubject> Subjects,
    IReadOnlyCollection<HistoricalFact> Facts,
    IReadOnlyDictionary<string, string> ZoneNames);
