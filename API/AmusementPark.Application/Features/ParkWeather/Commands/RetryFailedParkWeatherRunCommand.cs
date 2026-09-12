using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Results;

namespace AmusementPark.Application.Features.ParkWeather.Commands;

public sealed record RetryFailedParkWeatherRunCommand(string RunId) : ICommand<ApplicationResult<ParkWeatherRunResult>>;
