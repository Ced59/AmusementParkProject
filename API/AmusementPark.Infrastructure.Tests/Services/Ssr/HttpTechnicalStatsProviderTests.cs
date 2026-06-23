using System.Net;
using System.Text.Json;
using System.Text;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Services.Ssr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ssr;

public sealed class HttpTechnicalStatsProviderTests
{
    [Fact]
    public async Task GetSnapshotAsyncCallsInternalEndpointWithSharedToken()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(HttpStatusCode.OK, """
        {
          "generatedAtUtc": "2026-06-23T10:00:00Z",
          "startedAtUtc": "2026-06-23T09:00:00Z",
          "uptimeSeconds": 3600,
          "buildVersion": "2.6.18",
          "cache": {
            "pageResponses": 10,
            "cacheablePageResponses": 10,
            "cacheHitResponses": 7,
            "hitRatePercent": 70,
            "robotPageResponses": 4,
            "robotCacheHitResponses": 3,
            "robotHitRatePercent": 75,
            "statuses": [{ "key": "HIT", "count": 7, "percent": 70 }],
            "robotFamilies": [{ "key": "Googlebot", "count": 4, "cacheHits": 3, "hitRatePercent": 75 }]
          }
        }
        """);
        HttpClient httpClient = new HttpClient(handler);
        HttpTechnicalStatsProvider provider = CreateProvider(httpClient);

        TechnicalStatsSnapshot? snapshot = await provider.GetSnapshotAsync();

        Assert.NotNull(snapshot);
        Assert.Equal("2.6.18", snapshot.BuildVersion);
        Assert.Equal(70, snapshot.Cache.HitRatePercent);
        Assert.Equal("https://front.test/internal/technical-stats", handler.RequestUri?.ToString());
        Assert.Contains("secret-token", handler.CacheTokenHeaderValues);
    }

    [Fact]
    public async Task GetSnapshotAsyncReturnsUnavailableSnapshotWhenSettingsAreMissing()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(HttpStatusCode.OK, "{}");
        HttpTechnicalStatsProvider provider = new HttpTechnicalStatsProvider(
            new TestHttpClientFactory(new HttpClient(handler)),
            new SsrSettings(),
            NullLogger<HttpTechnicalStatsProvider>.Instance);

        TechnicalStatsSnapshot? snapshot = await provider.GetSnapshotAsync();

        Assert.NotNull(snapshot);
        Assert.False(snapshot.IsAvailable);
        Assert.Equal("missing-settings", snapshot.UnavailableReason);
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task GetSnapshotAsyncReturnsUnavailableSnapshotWhenSsrReturnsError()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(HttpStatusCode.Forbidden, "{}");
        HttpClient httpClient = new HttpClient(handler);
        HttpTechnicalStatsProvider provider = CreateProvider(httpClient);

        TechnicalStatsSnapshot? snapshot = await provider.GetSnapshotAsync();

        Assert.NotNull(snapshot);
        Assert.False(snapshot.IsAvailable);
        Assert.Equal("http-403", snapshot.UnavailableReason);
    }

    [Fact]
    public async Task UpdateSettingsAsyncCallsInternalEndpointWithSharedTokenAndBody()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(HttpStatusCode.OK, """
        {
          "persistenceRetentionDays": 20
        }
        """);
        HttpClient httpClient = new HttpClient(handler);
        HttpTechnicalStatsProvider provider = CreateProvider(httpClient);

        TechnicalStatsSettings? settings = await provider.UpdateSettingsAsync(new TechnicalStatsSettings { PersistenceRetentionDays = 20 });

        Assert.NotNull(settings);
        Assert.Equal(20, settings.PersistenceRetentionDays);
        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Equal("https://front.test/internal/technical-stats/settings", handler.RequestUri?.ToString());
        Assert.Contains("secret-token", handler.CacheTokenHeaderValues);
        Assert.NotNull(handler.RequestBody);
        using JsonDocument document = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal(20, document.RootElement.GetProperty("persistenceRetentionDays").GetInt32());
    }

    private static HttpTechnicalStatsProvider CreateProvider(HttpClient httpClient)
    {
        SsrSettings settings = new SsrSettings
        {
            InternalBaseUrl = "https://front.test/",
            CacheInvalidationToken = "secret-token"
        };

        return new HttpTechnicalStatsProvider(
            new TestHttpClientFactory(httpClient),
            settings,
            NullLogger<HttpTechnicalStatsProvider>.Instance);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient httpClient;

        public TestHttpClientFactory(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            Assert.Equal(HttpTechnicalStatsProvider.HttpClientName, name);
            return this.httpClient;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly string responseBody;

        public RecordingHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            this.statusCode = statusCode;
            this.responseBody = responseBody;
        }

        public Uri? RequestUri { get; private set; }

        public HttpMethod? Method { get; private set; }

        public string? RequestBody { get; private set; }

        public IReadOnlyCollection<string> CacheTokenHeaderValues { get; private set; } = Array.Empty<string>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.RequestUri = request.RequestUri;
            this.Method = request.Method;
            this.RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            this.CacheTokenHeaderValues = request.Headers.TryGetValues("X-AmusementPark-Cache-Token", out IEnumerable<string>? values)
                ? values.ToArray()
                : Array.Empty<string>();

            HttpResponseMessage response = new HttpResponseMessage(this.statusCode)
            {
                Content = new StringContent(this.responseBody, Encoding.UTF8, "application/json")
            };

            return response;
        }
    }
}
