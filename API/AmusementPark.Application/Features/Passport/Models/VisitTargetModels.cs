using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitTarget(
    string ParkItemId,
    string ParkId,
    string Name,
    ParkItemCategory Category,
    DateOnly? OpeningDate,
    DateOnly? ClosingDate);
