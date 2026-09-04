using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Passport.Queries;

public sealed record ValidateRideTargetsQuery(
    string UserId,
    string ParkId,
    IReadOnlyCollection<string?> ParkItemIds)
    : IQuery<ApplicationResult<bool>>;
