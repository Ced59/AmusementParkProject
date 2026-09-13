using System.Net;
using System.Text.Json;
using System.Text;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Services.Ssr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ssr;

internal sealed class HttpTechnicalStatsProviderTestsRecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode statusCode;
    private readonly string responseBody;

    public HttpTechnicalStatsProviderTestsRecordingHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
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
