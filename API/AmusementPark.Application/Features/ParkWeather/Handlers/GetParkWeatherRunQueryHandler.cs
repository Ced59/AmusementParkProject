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

public sealed class GetParkWeatherRunQueryHandler : IQueryHandler<GetParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult>>
{
    private readonly IParkWeatherRunRepository runRepository;

    public GetParkWeatherRunQueryHandler(IParkWeatherRunRepository runRepository)
    {
        this.runRepository = runRepository;
    }

    public async Task<ApplicationResult<ParkWeatherRunResult>> HandleAsync(GetParkWeatherRunQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.RunId))
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        ParkWeatherRun? run = await this.runRepository.GetByIdAsync(query.RunId.Trim(), cancellationToken);
        if (run is null)
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        return ApplicationResult<ParkWeatherRunResult>.Success(run.ToResult());
    }
}
