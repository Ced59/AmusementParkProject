using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Infrastructure.Services.Seo;

/// <summary>
/// Client HTTP IndexNow compatible Bing/IndexNow.
/// </summary>
public sealed class IndexNowSubmitter : IIndexNowSubmitter
{
    private readonly IHttpClientFactory httpClientFactory;

    public IndexNowSubmitter(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<IndexNowSubmissionResult> SubmitAsync(SeoSitemapSettings settings, string publicBaseUrl, IReadOnlyCollection<string> absoluteUrls, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsIndexNowEnabled)
        {
            return new IndexNowSubmissionResult
            {
                WasRequested = true,
                IsEnabled = false,
                IsSuccess = false,
                Errors = new[] { "IndexNow est désactivé." },
            };
        }

        if (string.IsNullOrWhiteSpace(settings.IndexNowKey))
        {
            return new IndexNowSubmissionResult
            {
                WasRequested = true,
                IsEnabled = true,
                IsSuccess = false,
                Errors = new[] { "La clé IndexNow est absente." },
            };
        }

        List<string> urls = absoluteUrls
            .Where(static url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10000)
            .ToList();

        if (urls.Count == 0)
        {
            return new IndexNowSubmissionResult
            {
                WasRequested = true,
                IsEnabled = true,
                IsSuccess = true,
                SubmittedUrlCount = 0,
            };
        }

        Uri hostUri = new Uri(publicBaseUrl.TrimEnd('/'));
        string keyLocation = NormalizeKeyLocation(publicBaseUrl, settings);
        IReadOnlyCollection<string> endpoints = NormalizeEndpoints(settings.IndexNowEndpoints);
        List<string> acceptedEndpoints = new List<string>();
        List<string> errors = new List<string>();

        foreach (string endpoint in endpoints)
        {
            try
            {
                HttpClient client = this.httpClientFactory.CreateClient();
                IndexNowPayload payload = new IndexNowPayload
                {
                    Host = hostUri.Host,
                    Key = settings.IndexNowKey.Trim(),
                    KeyLocation = keyLocation,
                    UrlList = urls,
                };

                HttpResponseMessage response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    acceptedEndpoints.Add(endpoint);
                }
                else
                {
                    errors.Add($"{endpoint} -> {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors.Add($"{endpoint} -> {exception.Message}");
            }
        }

        return new IndexNowSubmissionResult
        {
            WasRequested = true,
            IsEnabled = true,
            IsSuccess = acceptedEndpoints.Count > 0 && errors.Count == 0,
            SubmittedUrlCount = urls.Count,
            AcceptedEndpoints = acceptedEndpoints,
            Errors = errors,
        };
    }

    private static string NormalizeKeyLocation(string publicBaseUrl, SeoSitemapSettings settings)
    {
        string normalizedPublicBaseUrl = publicBaseUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(settings.IndexNowKeyLocation))
        {
            string value = settings.IndexNowKeyLocation.Trim();
            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            string normalizedPath = value.StartsWith('/') ? value : $"/{value}";
            return $"{normalizedPublicBaseUrl}{normalizedPath}";
        }

        return $"{normalizedPublicBaseUrl}/{settings.IndexNowKey.Trim()}.txt";
    }

    private static IReadOnlyCollection<string> NormalizeEndpoints(IReadOnlyCollection<string> endpoints)
    {
        List<string> normalized = endpoints
            .Where(static endpoint => !string.IsNullOrWhiteSpace(endpoint))
            .Select(static endpoint => endpoint.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            normalized.Add("https://api.indexnow.org/indexnow");
            normalized.Add("https://www.bing.com/indexnow");
        }

        return normalized;
    }

    private sealed class IndexNowPayload
    {
        [JsonPropertyName("host")]
        public string Host { get; init; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; init; } = string.Empty;

        [JsonPropertyName("keyLocation")]
        public string KeyLocation { get; init; } = string.Empty;

        [JsonPropertyName("urlList")]
        public IReadOnlyCollection<string> UrlList { get; init; } = Array.Empty<string>();
    }
}
