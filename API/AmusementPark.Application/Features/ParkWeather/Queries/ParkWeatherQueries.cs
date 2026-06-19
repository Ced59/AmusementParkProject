using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Results;

namespace AmusementPark.Application.Features.ParkWeather.Queries;

public sealed record GetParkWeatherForecastQuery(string ParkId, int DayCount) : IQuery<ApplicationResult<ParkWeatherForecastResult>>;

public sealed record GetLatestParkWeatherRunQuery() : IQuery<ApplicationResult<ParkWeatherRunResult?>>;

public sealed record GetParkWeatherRunQuery(string RunId) : IQuery<ApplicationResult<ParkWeatherRunResult>>;

public sealed record GetParkWeatherRunItemsQuery(string RunId, string? Status) : IQuery<ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>>;
