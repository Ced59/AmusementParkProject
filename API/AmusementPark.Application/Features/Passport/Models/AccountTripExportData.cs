using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountTripExportData(
    string Reference,
    string MembershipRole,
    TripExportResult Trip);
