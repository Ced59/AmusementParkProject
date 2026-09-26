using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed record PublicParkHistoricalScope(
    Park Park,
    IReadOnlyCollection<HistoricalSubject> PublicCurrentSubjects,
    IReadOnlyDictionary<string, string> ZoneNames);
