using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalParkEditorialScope(
    string ParkId,
    string ParkName,
    bool IsPublicPark,
    IReadOnlyCollection<HistoricalSubject> CurrentSubjects,
    IReadOnlyCollection<HistoricalSubject> PublicCurrentSubjects,
    IReadOnlyCollection<string> CurrentZoneIds);
