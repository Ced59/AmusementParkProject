using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Results;

namespace AmusementPark.Application.Features.ParkWeather.Queries;

public sealed record GetParkWeatherRunItemsQuery(string RunId, string? Status) : IQuery<ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>>;
