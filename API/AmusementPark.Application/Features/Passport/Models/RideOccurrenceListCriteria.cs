using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceListCriteria(
    VisitId VisitId,
    string UserId,
    int Limit,
    RideOccurrenceListCursor? After = null)
{
    public const int DefaultLimit = 100;

    public const int MaximumLimit = 250;
}
