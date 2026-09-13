using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;

namespace AmusementPark.Infrastructure.Services.Weather;

internal sealed record DateRange(DateOnly Start, DateOnly End);
