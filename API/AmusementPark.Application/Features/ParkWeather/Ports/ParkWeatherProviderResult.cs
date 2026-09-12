using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public sealed class ParkWeatherProviderResult
{
    public IReadOnlyCollection<ParkWeatherDailySnapshot> Snapshots { get; init; } = Array.Empty<ParkWeatherDailySnapshot>();

    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
}
