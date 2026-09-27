using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportHistoricalTargetContext(
    IReadOnlyDictionary<string, PassportHistoricalTarget> Targets,
    HistoricalCoverageStatus CoverageStatus,
    int CoveragePercent,
    string MethodologyVersion);
