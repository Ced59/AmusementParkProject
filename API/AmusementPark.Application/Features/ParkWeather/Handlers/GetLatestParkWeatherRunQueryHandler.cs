using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Queries;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Handlers;

public sealed class GetLatestParkWeatherRunQueryHandler : IQueryHandler<GetLatestParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult?>>
{
    private readonly IParkWeatherRunRepository runRepository;

    public GetLatestParkWeatherRunQueryHandler(IParkWeatherRunRepository runRepository)
    {
        this.runRepository = runRepository;
    }

    public async Task<ApplicationResult<ParkWeatherRunResult?>> HandleAsync(GetLatestParkWeatherRunQuery query, CancellationToken cancellationToken = default)
    {
        ParkWeatherRun? run = await this.runRepository.GetLatestAsync(cancellationToken);
        return ApplicationResult<ParkWeatherRunResult?>.Success(run?.ToResult());
    }
}
