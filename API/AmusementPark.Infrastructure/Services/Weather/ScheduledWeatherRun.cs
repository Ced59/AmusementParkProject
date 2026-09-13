using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Weather;

internal sealed record ScheduledWeatherRun(DateOnly LocalDate, TimeSpan Delay);
