using System.Net;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;
using AmusementPark.Infrastructure.Services.SocialPublishing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.SocialPublishing;

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode statusCode;
    private readonly string responseBody;
    private readonly bool pageSeoReady;
    private readonly bool failPageBodyRead;
    private readonly Queue<string> graphResponseBodies;

    public RecordingHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseBody,
        bool pageSeoReady = true,
        bool failPageBodyRead = false,
        IReadOnlyCollection<string>? graphResponseBodies = null)
    {
        this.statusCode = statusCode;
        this.responseBody = responseBody;
        this.pageSeoReady = pageSeoReady;
        this.failPageBodyRead = failPageBodyRead;
        this.graphResponseBodies = new Queue<string>(graphResponseBodies ?? Array.Empty<string>());
    }

    public List<string> RequestUris { get; } = new List<string>();

    public int PagePreparationCallCount { get; private set; }

    public bool PagePreparationBodyRead { get; private set; }

    public int GraphCallCount { get; private set; }

    public bool WarmupRequested { get; private set; }

    public bool WarmupRefreshRequested { get; private set; }

    public string? RequestUri { get; private set; }

    public string? AuthorizationScheme { get; private set; }

    public string? AuthorizationParameter { get; private set; }

    public string RequestBody { get; private set; } = string.Empty;

    public HttpMethod? Method { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string requestUri = request.RequestUri?.AbsoluteUri ?? string.Empty;
        this.RequestUris.Add(requestUri);
        if (request.Method == HttpMethod.Get
            && request.RequestUri?.Host == "amusement-parks.fun")
        {
            this.PagePreparationCallCount++;
            this.WarmupRequested = request.Headers.TryGetValues(
                    "X-AmusementPark-SSR-Warmup",
                    out IEnumerable<string>? warmupValues)
                && warmupValues.Contains("1", StringComparer.Ordinal);
            this.WarmupRefreshRequested = request.Headers.TryGetValues(
                    "X-AmusementPark-SSR-Warmup-Refresh",
                    out IEnumerable<string>? refreshValues)
                && refreshValues.Contains("1", StringComparer.Ordinal);
            HttpResponseMessage preparationResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new TrackingStringContent(
                    "<html></html>",
                    () =>
                    {
                        this.PagePreparationBodyRead = true;
                        if (this.failPageBodyRead)
                        {
                            throw new IOException("Simulated incomplete HTTP response body.");
                        }
                    }),
            };
            preparationResponse.Headers.TryAddWithoutValidation(
                "X-AmusementPark-Seo-Ready",
                this.pageSeoReady ? "true" : "false");
            return preparationResponse;
        }

        this.GraphCallCount++;
        this.RequestUri = request.RequestUri?.AbsoluteUri;
        this.Method = request.Method;
        this.AuthorizationScheme = request.Headers.Authorization?.Scheme;
        this.AuthorizationParameter = request.Headers.Authorization?.Parameter;
        this.RequestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(this.statusCode)
        {
            Content = new StringContent(
                this.graphResponseBodies.Count > 0
                    ? this.graphResponseBodies.Dequeue()
                    : this.responseBody),
        };
    }
}
