using System.Net.Http.Json;
using System.Text.Json;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Infrastructure.Configuration.Ssr;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Ssr;

public sealed class HttpTechnicalStatsProvider : ITechnicalStatsProvider
{
    public const string HttpClientName = "ssr-technical-stats";

    private const string TokenHeaderName = "X-AmusementPark-Cache-Token";
    private const string TechnicalStatsPath = "/internal/technical-stats";
    private const string TechnicalStatsSettingsPath = "/internal/technical-stats/settings";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly SsrSettings settings;
    private readonly ILogger<HttpTechnicalStatsProvider> logger;

    public HttpTechnicalStatsProvider(
        IHttpClientFactory httpClientFactory,
        SsrSettings settings,
        ILogger<HttpTechnicalStatsProvider> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<TechnicalStatsSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this.settings.InternalBaseUrl) || string.IsNullOrWhiteSpace(this.settings.CacheInvalidationToken))
        {
            this.logger.LogDebug("SSR technical stats skipped: SSR internal base URL or token is not configured.");
            return BuildUnavailableSnapshot("missing-settings");
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUri = BuildTechnicalStatsUri(this.settings.InternalBaseUrl);

            using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(HttpMethod.Get, requestUri);

            using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("SSR technical stats returned HTTP {StatusCode}.", (int)response.StatusCode);
                return BuildUnavailableSnapshot($"http-{(int)response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<TechnicalStatsSnapshot>(JsonSerializerOptions.Web, cancellationToken)
                ?? BuildUnavailableSnapshot("empty-response");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            this.logger.LogWarning(exception, "SSR technical stats request timed out.");
            return BuildUnavailableSnapshot("timeout");
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats request failed.");
            return BuildUnavailableSnapshot("request-failed");
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats request failed.");
            return BuildUnavailableSnapshot("unexpected-error");
        }
    }

    public async Task<TechnicalStatsSettings?> UpdateSettingsAsync(TechnicalStatsSettings settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this.settings.InternalBaseUrl) || string.IsNullOrWhiteSpace(this.settings.CacheInvalidationToken))
        {
            this.logger.LogDebug("SSR technical stats settings skipped: SSR internal base URL or token is not configured.");
            return null;
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUri = BuildTechnicalStatsSettingsUri(this.settings.InternalBaseUrl);

            using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(HttpMethod.Put, requestUri);
            httpRequest.Content = JsonContent.Create(settings, options: JsonSerializerOptions.Web);

            using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("SSR technical stats settings returned HTTP {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TechnicalStatsSettings>(JsonSerializerOptions.Web, cancellationToken);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats settings request failed.");
            return null;
        }
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
    {
        HttpRequestMessage httpRequest = new HttpRequestMessage(method, requestUri);
        httpRequest.Headers.TryAddWithoutValidation(TokenHeaderName, this.settings.CacheInvalidationToken);
        return httpRequest;
    }

    private static string BuildTechnicalStatsUri(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{TechnicalStatsPath}";
    }

    private static string BuildTechnicalStatsSettingsUri(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{TechnicalStatsSettingsPath}";
    }

    private static TechnicalStatsSnapshot BuildUnavailableSnapshot(string reason)
    {
        DateTime now = DateTime.UtcNow;

        return new TechnicalStatsSnapshot
        {
            IsAvailable = false,
            UnavailableReason = reason,
            GeneratedAtUtc = now,
            StartedAtUtc = now,
            UptimeSeconds = 0
        };
    }
}
