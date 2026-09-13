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
        HttpSsrPageCacheInvalidatorTestsRecordingHttpMessageHandler handler = new HttpSsrPageCacheInvalidatorTestsRecordingHttpMessageHandler(HttpStatusCode.NoContent);
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
        HttpSsrPageCacheInvalidatorTestsRecordingHttpMessageHandler handler = new HttpSsrPageCacheInvalidatorTestsRecordingHttpMessageHandler(HttpStatusCode.ServiceUnavailable);
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




}
