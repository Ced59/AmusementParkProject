using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceCreationPreparation(
    string ParkId,
    VisitDate VisitDate,
    string? TimeZoneId,
    LocalServiceDayConvention ServiceDayConvention,
    IReadOnlyList<HistoricalConsistency> HistoricalConsistencies);
