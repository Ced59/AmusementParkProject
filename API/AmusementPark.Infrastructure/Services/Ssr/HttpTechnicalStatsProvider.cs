using System.Net.Http.Json;
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
            return null;
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUri = BuildTechnicalStatsUri(this.settings.InternalBaseUrl);

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.TryAddWithoutValidation(TokenHeaderName, this.settings.CacheInvalidationToken);

            using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("SSR technical stats returned HTTP {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TechnicalStatsSnapshot>(cancellationToken);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats request failed.");
            return null;
        }
    }

    private static string BuildTechnicalStatsUri(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{TechnicalStatsPath}";
    }
}
