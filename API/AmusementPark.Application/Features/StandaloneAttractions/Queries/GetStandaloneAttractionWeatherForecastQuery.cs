using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Results;

namespace AmusementPark.Application.Features.StandaloneAttractions.Queries;

public sealed record GetStandaloneAttractionWeatherForecastQuery(
    string StandaloneAttractionId,
    int DayCount) : IQuery<ApplicationResult<ParkWeatherForecastResult>>;
