using System.Net.Http.Headers;

namespace AmusementPark.Infrastructure.Services.DataSources.Acquisition;

/// <summary>
/// Télécharge du contenu texte sans persister les payloads sur disque.
/// </summary>
internal interface IDataAcquisitionHttpFetcher
{
    Task<string> GetStringAsync(string url, string acceptLanguage, DataAcquisitionRequestOptions options, CancellationToken cancellationToken);
}

/// <summary>
/// Implémentation HTTP générique réutilisable par plusieurs providers de sources externes.
/// </summary>
internal sealed class DataAcquisitionHttpFetcher : IDataAcquisitionHttpFetcher
{
    private readonly IHttpClientFactory httpClientFactory;

    public DataAcquisitionHttpFetcher(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetStringAsync(string url, string acceptLanguage, DataAcquisitionRequestOptions options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(options);

        HttpClient httpClient = this.httpClientFactory.CreateClient(nameof(DataAcquisitionHttpFetcher));
        httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 300));

        Exception? lastException = null;

        for (int attempt = 1; attempt <= Math.Max(1, options.MaxRetryCount); attempt++)
        {
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.Clear();
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AmusementParkDataAcquisition", "1.0"));
                request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                if (!string.IsNullOrWhiteSpace(acceptLanguage))
                {
                    request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
                }

                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (options.DelayBetweenRequestsMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(options.DelayBetweenRequestsMs), cancellationToken);
                }

                return content;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt < Math.Max(1, options.MaxRetryCount))
            {
                lastException = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
            catch (Exception exception)
            {
                lastException = exception;
                break;
            }
        }

        throw new InvalidOperationException($"Impossible de récupérer la ressource '{url}'.", lastException);
    }
}

/// <summary>
/// Options réseau génériques d'acquisition.
/// </summary>
internal sealed class DataAcquisitionRequestOptions
{
    public int DelayBetweenRequestsMs { get; init; } = 1000;

    public int TimeoutSeconds { get; init; } = 30;

    public int MaxRetryCount { get; init; } = 3;
}
