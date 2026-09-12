using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record UserVisitListCriteria(
    string UserId,
    int Limit,
    string? ParkId = null,
    int? Year = null,
    VisitStatus? Status = null,
    UserVisitListCursor? After = null)
{
    public const int DefaultLimit = 25;

    public const int MaximumLimit = 100;
}
