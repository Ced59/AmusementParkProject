using System.Net;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Services.Ssr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ssr;

internal sealed class HttpSsrPageCacheInvalidatorTestsRecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode statusCode;

    public HttpSsrPageCacheInvalidatorTestsRecordingHttpMessageHandler(HttpStatusCode statusCode)
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
