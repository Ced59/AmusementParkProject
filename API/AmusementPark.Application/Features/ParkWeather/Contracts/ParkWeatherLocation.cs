using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.ParkWeather.Contracts;

public sealed record ParkWeatherLocation(string Id, string Name, GeoPoint Position);
