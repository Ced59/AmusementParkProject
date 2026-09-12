using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Commands;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;

namespace AmusementPark.Application.Features.ParkWeather.Handlers;

public sealed class StartParkWeatherManualRefreshCommandHandler : ICommandHandler<StartParkWeatherManualRefreshCommand, ApplicationResult<ParkWeatherRunResult>>
{
    private readonly ParkWeatherRefreshStarter starter;

    public StartParkWeatherManualRefreshCommandHandler(ParkWeatherRefreshStarter starter)
    {
        this.starter = starter;
    }

    public Task<ApplicationResult<ParkWeatherRunResult>> HandleAsync(StartParkWeatherManualRefreshCommand command, CancellationToken cancellationToken = default)
    {
        return this.starter.StartManualFullRefreshAsync(cancellationToken);
    }
}
