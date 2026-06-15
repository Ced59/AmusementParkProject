using System.Net.Http;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Ssr;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Ssr;

/// <summary>
/// Notifie le serveur SSR (Node) de purger son cache de pages via un appel HTTP
/// interne authentifié par un jeton partagé. Toute erreur est journalisée mais
/// jamais propagée : l'invalidation est un effet de bord opportuniste qui ne
/// doit pas faire échouer l'écriture métier.
/// </summary>
public sealed class HttpSsrPageCacheInvalidator : ISsrPageCacheInvalidator
{
    public const string HttpClientName = "ssr-cache-invalidation";

    private const string TokenHeaderName = "X-AmusementPark-Cache-Token";
    private const string InvalidationPath = "/internal/cache/invalidate";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly SsrSettings settings;
    private readonly ILogger<HttpSsrPageCacheInvalidator> logger;

    public HttpSsrPageCacheInvalidator(
        IHttpClientFactory httpClientFactory,
        SsrSettings settings,
        ILogger<HttpSsrPageCacheInvalidator> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this.settings.InternalBaseUrl) || string.IsNullOrWhiteSpace(this.settings.CacheInvalidationToken))
        {
            this.logger.LogDebug("SSR page cache invalidation skipped: SSR internal base URL or token is not configured.");
            return;
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUri = BuildInvalidationUri(this.settings.InternalBaseUrl);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.TryAddWithoutValidation(TokenHeaderName, this.settings.CacheInvalidationToken);

            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("SSR page cache invalidation returned HTTP {StatusCode}.", (int)response.StatusCode);
            }
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR page cache invalidation request failed.");
        }
    }

    private static string BuildInvalidationUri(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{InvalidationPath}";
    }
}
