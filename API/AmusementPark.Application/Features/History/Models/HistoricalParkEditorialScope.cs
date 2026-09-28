using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalParkEditorialScope(
    string ParkId,
    string ParkName,
    IReadOnlyCollection<HistoricalSubject> CurrentSubjects,
    IReadOnlyCollection<string> CurrentZoneIds);
