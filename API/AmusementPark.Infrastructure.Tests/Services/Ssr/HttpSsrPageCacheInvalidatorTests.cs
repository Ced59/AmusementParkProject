using System.Net;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Services.Ssr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ssr;

public sealed class HttpSsrPageCacheInvalidatorTests
{
    [Fact]
    public async Task TryInvalidateAsync_WhenSsrConfirmsPurge_ShouldReturnTrueAndAuthenticateRequest()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(HttpStatusCode.NoContent);
        HttpSsrPageCacheInvalidator invalidator = CreateInvalidator(handler);

        bool result = await invalidator.TryInvalidateAsync(
            SsrPageCacheInvalidationRequest.AllCaches(),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal("http://front:4000/internal/cache/invalidate", handler.RequestUri);
        Assert.Equal("test-token", handler.Token);
        Assert.Contains("\"all\":true", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"allowStale\":false", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryInvalidateAsync_WhenSsrRejectsPurge_ShouldReturnFalse()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(HttpStatusCode.ServiceUnavailable);
        HttpSsrPageCacheInvalidator invalidator = CreateInvalidator(handler);

        bool result = await invalidator.TryInvalidateAsync(
            SsrPageCacheInvalidationRequest.AllCaches(),
            CancellationToken.None);

        Assert.False(result);
    }

    private static HttpSsrPageCacheInvalidator CreateInvalidator(HttpMessageHandler handler)
    {
        SsrSettings settings = new SsrSettings
        {
            InternalBaseUrl = "http://front:4000/",
            CacheInvalidationToken = "test-token",
        };
        return new HttpSsrPageCacheInvalidator(
            new StubHttpClientFactory(handler),
            settings,
            NullLogger<HttpSsrPageCacheInvalidator>.Instance);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            this.handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(this.handler, disposeHandler: false);
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;

        public RecordingHttpMessageHandler(HttpStatusCode statusCode)
        {
            this.statusCode = statusCode;
        }

        public string? RequestUri { get; private set; }

        public string? Token { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.RequestUri = request.RequestUri?.ToString();
            this.Token = request.Headers.GetValues("X-AmusementPark-Cache-Token").Single();
            this.Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(this.statusCode);
        }
    }
}
