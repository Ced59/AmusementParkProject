using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Results;

namespace AmusementPark.Application.Features.LiveData.Queries;

public sealed record GetAdminLiveWaitForecastBacktestQuery(
    string ParkItemId,
    DateTimeOffset? From,
    DateTimeOffset? To)
    : IQuery<ApplicationResult<LiveWaitForecastBacktestResult>>;
