using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Commands;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;

namespace AmusementPark.Application.Features.ParkWeather.Handlers;

public sealed class RefreshSingleParkWeatherCommandHandler : ICommandHandler<RefreshSingleParkWeatherCommand, ApplicationResult<ParkWeatherRunResult>>
{
    private readonly ParkWeatherRefreshStarter starter;

    public RefreshSingleParkWeatherCommandHandler(ParkWeatherRefreshStarter starter)
    {
        this.starter = starter;
    }

    public Task<ApplicationResult<ParkWeatherRunResult>> HandleAsync(RefreshSingleParkWeatherCommand command, CancellationToken cancellationToken = default)
    {
        return this.starter.StartSingleParkRefreshAsync(command.ParkId, cancellationToken);
    }
}
