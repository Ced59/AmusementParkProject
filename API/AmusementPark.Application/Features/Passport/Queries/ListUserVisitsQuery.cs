using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Queries;

public sealed record ListUserVisitsQuery(
    string UserId,
    int Limit = UserVisitListCriteria.DefaultLimit,
    string? ParkId = null,
    int? Year = null,
    VisitStatus? Status = null,
    UserVisitListCursor? After = null) : IQuery<ApplicationResult<VisitPageResult>>;
