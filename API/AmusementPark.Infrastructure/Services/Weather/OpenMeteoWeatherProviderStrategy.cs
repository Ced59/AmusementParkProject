using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;

namespace AmusementPark.Infrastructure.Services.Weather;

public sealed class OpenMeteoWeatherProviderStrategy : IParkWeatherProviderStrategy
{
    public const string HttpClientName = "open-meteo-weather";

    private const string Provider = "open-meteo";
    private static readonly string DailyForecastVariables = string.Join(",", new[]
    {
        "weather_code",
        "temperature_2m_max",
        "temperature_2m_min",
        "apparent_temperature_max",
        "apparent_temperature_min",
        "precipitation_probability_max",
        "precipitation_sum",
        "wind_speed_10m_max",
        "wind_gusts_10m_max",
    });

    private static readonly string DailyArchiveVariables = string.Join(",", new[]
    {
        "weather_code",
        "temperature_2m_max",
        "temperature_2m_min",
        "apparent_temperature_max",
        "apparent_temperature_min",
        "precipitation_sum",
        "wind_speed_10m_max",
        "wind_gusts_10m_max",
    });

    private readonly IHttpClientFactory httpClientFactory;
    private readonly ParkWeatherSettings settings;

    public OpenMeteoWeatherProviderStrategy(IHttpClientFactory httpClientFactory, ParkWeatherSettings settings)
    {
        this.httpClientFactory = httpClientFactory;
        this.settings = settings;
    }

    public string ProviderKey => Provider;

    public async Task<ParkWeatherProviderResult> FetchDailyForecastAsync(
        Park park,
        int forecastDays,
        bool includeYesterdayObservation,
        CancellationToken cancellationToken)
    {
        if (park.Position is null)
        {
            throw new InvalidOperationException($"Park '{park.Id}' has no coordinates.");
        }

        List<ParkWeatherDailySnapshot> snapshots = new List<ParkWeatherDailySnapshot>();
        List<string> warnings = new List<string>();
        HttpClient httpClient = this.httpClientFactory.CreateClient(HttpClientName);

        OpenMeteoResponse forecastResponse = await this.GetAsync(
            httpClient,
            this.BuildForecastUrl(park.Position.Latitude, park.Position.Longitude, forecastDays),
            cancellationToken);

        DateTime forecastFetchedAtUtc = DateTime.UtcNow;
        IReadOnlyCollection<ParkWeatherDailySnapshot> forecastSnapshots = this.MapDailySnapshots(
            park,
            forecastResponse,
            ParkWeatherDataKind.Forecast,
            forecastFetchedAtUtc);
        snapshots.AddRange(forecastSnapshots);

        if (includeYesterdayObservation)
        {
            DateOnly? firstForecastLocalDate = ResolveFirstLocalDate(forecastResponse);
            if (!firstForecastLocalDate.HasValue)
            {
                warnings.Add("Yesterday observation could not be fetched because Open-Meteo did not return a local forecast date.");
            }
            else
            {
                try
                {
                    DateOnly yesterday = firstForecastLocalDate.Value.AddDays(-1);
                    OpenMeteoResponse archiveResponse = await this.GetAsync(
                        httpClient,
                        this.BuildArchiveUrl(park.Position.Latitude, park.Position.Longitude, yesterday),
                        cancellationToken);

                    snapshots.AddRange(this.MapDailySnapshots(
                        park,
                        archiveResponse,
                        ParkWeatherDataKind.Observation,
                        DateTime.UtcNow));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    warnings.Add($"Yesterday observation could not be fetched: {SanitizeWarning(exception.Message)}");
                }
            }
        }

        return new ParkWeatherProviderResult
        {
            Snapshots = snapshots,
            Warnings = warnings,
        };
    }

    private async Task<OpenMeteoResponse> GetAsync(HttpClient httpClient, string url, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Open-Meteo returned HTTP {(int)response.StatusCode}.");
        }

        OpenMeteoResponse? payload = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>(cancellationToken);
        if (payload is null || payload.Daily is null)
        {
            throw new InvalidOperationException("Open-Meteo returned an empty daily weather response.");
        }

        return payload;
    }

    private string BuildForecastUrl(double latitude, double longitude, int forecastDays)
    {
        Dictionary<string, string> query = new Dictionary<string, string>
        {
            ["latitude"] = FormatNumber(latitude),
            ["longitude"] = FormatNumber(longitude),
            ["daily"] = DailyForecastVariables,
            ["forecast_days"] = Math.Clamp(forecastDays, 1, 7).ToString(CultureInfo.InvariantCulture),
            ["timezone"] = "auto",
        };

        return BuildUrl(this.settings.OpenMeteoForecastBaseUrl, query);
    }

    private string BuildArchiveUrl(double latitude, double longitude, DateOnly date)
    {
        string formattedDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Dictionary<string, string> query = new Dictionary<string, string>
        {
            ["latitude"] = FormatNumber(latitude),
            ["longitude"] = FormatNumber(longitude),
            ["start_date"] = formattedDate,
            ["end_date"] = formattedDate,
            ["daily"] = DailyArchiveVariables,
            ["timezone"] = "auto",
        };

        return BuildUrl(this.settings.OpenMeteoArchiveBaseUrl, query);
    }

    private IReadOnlyCollection<ParkWeatherDailySnapshot> MapDailySnapshots(
        Park park,
        OpenMeteoResponse response,
        ParkWeatherDataKind dataKind,
        DateTime fetchedAtUtc)
    {
        List<ParkWeatherDailySnapshot> snapshots = new List<ParkWeatherDailySnapshot>();
        IReadOnlyList<string> times = response.Daily?.Time is { } dailyTimes
            ? dailyTimes
            : Array.Empty<string>();

        for (int index = 0; index < times.Count; index += 1)
        {
            if (!DateOnly.TryParseExact(times[index], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly localDate))
            {
                continue;
            }

            snapshots.Add(new ParkWeatherDailySnapshot
            {
                ParkId = park.Id,
                LocalDate = localDate,
                DataKind = dataKind,
                SourceProvider = Provider,
                FetchedAtUtc = fetchedAtUtc,
                TimeZone = response.TimeZone,
                UtcOffsetSeconds = response.UtcOffsetSeconds,
                Latitude = response.Latitude ?? park.Position?.Latitude ?? 0d,
                Longitude = response.Longitude ?? park.Position?.Longitude ?? 0d,
                WeatherCode = GetAt(response.Daily?.WeatherCode, index),
                TemperatureMinCelsius = GetAt(response.Daily?.TemperatureMinCelsius, index),
                TemperatureMaxCelsius = GetAt(response.Daily?.TemperatureMaxCelsius, index),
                ApparentTemperatureMinCelsius = GetAt(response.Daily?.ApparentTemperatureMinCelsius, index),
                ApparentTemperatureMaxCelsius = GetAt(response.Daily?.ApparentTemperatureMaxCelsius, index),
                PrecipitationProbabilityMaxPercent = GetAt(response.Daily?.PrecipitationProbabilityMaxPercent, index),
                PrecipitationSumMillimeters = GetAt(response.Daily?.PrecipitationSumMillimeters, index),
                WindSpeedMaxKilometersPerHour = GetAt(response.Daily?.WindSpeedMaxKilometersPerHour, index),
                WindGustsMaxKilometersPerHour = GetAt(response.Daily?.WindGustsMaxKilometersPerHour, index),
            });
        }

        return snapshots;
    }

    private static DateOnly? ResolveFirstLocalDate(OpenMeteoResponse response)
    {
        string? firstTime = response.Daily?.Time?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstTime))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(firstTime, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly localDate))
        {
            return null;
        }

        return localDate;
    }

    private static string BuildUrl(string baseUrl, IReadOnlyDictionary<string, string> query)
    {
        string separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        string queryString = string.Join("&", query.Select(static item =>
            $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));

        return $"{baseUrl}{separator}{queryString}";
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static T? GetAt<T>(IReadOnlyList<T?>? values, int index)
        where T : struct
    {
        if (values is null || index < 0 || index >= values.Count)
        {
            return null;
        }

        return values[index];
    }

    private static string SanitizeWarning(string message)
    {
        string normalizedMessage = string.IsNullOrWhiteSpace(message) ? "unknown error" : message.Trim();
        return normalizedMessage.Length <= 200 ? normalizedMessage : normalizedMessage[..200];
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("timezone")]
        public string? TimeZone { get; set; }

        [JsonPropertyName("utc_offset_seconds")]
        public int? UtcOffsetSeconds { get; set; }

        [JsonPropertyName("daily")]
        public OpenMeteoDailyResponse? Daily { get; set; }
    }

    private sealed class OpenMeteoDailyResponse
    {
        [JsonPropertyName("time")]
        public List<string>? Time { get; set; }

        [JsonPropertyName("weather_code")]
        public List<int?>? WeatherCode { get; set; }

        [JsonPropertyName("temperature_2m_min")]
        public List<double?>? TemperatureMinCelsius { get; set; }

        [JsonPropertyName("temperature_2m_max")]
        public List<double?>? TemperatureMaxCelsius { get; set; }

        [JsonPropertyName("apparent_temperature_min")]
        public List<double?>? ApparentTemperatureMinCelsius { get; set; }

        [JsonPropertyName("apparent_temperature_max")]
        public List<double?>? ApparentTemperatureMaxCelsius { get; set; }

        [JsonPropertyName("precipitation_probability_max")]
        public List<int?>? PrecipitationProbabilityMaxPercent { get; set; }

        [JsonPropertyName("precipitation_sum")]
        public List<double?>? PrecipitationSumMillimeters { get; set; }

        [JsonPropertyName("wind_speed_10m_max")]
        public List<double?>? WindSpeedMaxKilometersPerHour { get; set; }

        [JsonPropertyName("wind_gusts_10m_max")]
        public List<double?>? WindGustsMaxKilometersPerHour { get; set; }
    }
}
