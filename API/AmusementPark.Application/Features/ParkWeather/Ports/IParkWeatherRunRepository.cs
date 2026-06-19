using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherRunRepository
{
    Task<ParkWeatherRun> CreateAsync(ParkWeatherRun run, CancellationToken cancellationToken);

    Task<ParkWeatherRun?> GetByIdAsync(string runId, CancellationToken cancellationToken);

    Task<ParkWeatherRun?> GetLatestAsync(CancellationToken cancellationToken);

    Task<bool> HasActiveRunAsync(CancellationToken cancellationToken);

    Task<bool> HasAutomaticCancellationAsync(DateOnly automaticRunLocalDate, CancellationToken cancellationToken);

    Task UpdateAsync(ParkWeatherRun run, CancellationToken cancellationToken);

    Task UpsertItemAsync(ParkWeatherRunItem item, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkWeatherRunItem>> GetRunItemsAsync(string runId, ParkWeatherRunItemStatus? status, CancellationToken cancellationToken);
}
