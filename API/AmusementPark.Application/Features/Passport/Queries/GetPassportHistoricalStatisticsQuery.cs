using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Queries;

public sealed record GetPassportHistoricalStatisticsQuery(string UserId)
    : IQuery<ApplicationResult<PassportHistoricalStatisticsResult>>;
