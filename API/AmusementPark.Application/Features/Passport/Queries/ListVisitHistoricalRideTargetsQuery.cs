using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Queries;

public sealed record ListVisitHistoricalRideTargetsQuery(
    string UserId,
    string VisitId,
    string? Search,
    VisitHistoricalTargetScope Scope,
    string? ZoneId,
    int Page,
    int PageSize)
    : IQuery<ApplicationResult<VisitHistoricalRideTargetPageResult>>;
