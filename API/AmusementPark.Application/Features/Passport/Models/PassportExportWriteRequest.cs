using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportExportWriteRequest(
    string ExportId,
    PassportExportFormat Format,
    DateTime ExportedAtUtc,
    IReadOnlyCollection<Visit> Visits,
    IReadOnlyCollection<RideOccurrence> RideOccurrences,
    IReadOnlyDictionary<string, Park> Parks,
    IReadOnlyDictionary<string, VisitTarget> ParkItems);
