using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Queries;

public sealed record ListRideOccurrencesQuery(
    string UserId,
    string VisitId,
    int Limit = RideOccurrenceListCriteria.DefaultLimit,
    RideOccurrenceListCursor? After = null)
    : IQuery<ApplicationResult<RideOccurrencePageResult>>;
