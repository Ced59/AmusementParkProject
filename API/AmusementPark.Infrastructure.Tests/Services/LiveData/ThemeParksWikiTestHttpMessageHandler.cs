using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AmusementPark.Infrastructure.Tests.Services.LiveData;

internal sealed class ThemeParksWikiTestHttpMessageHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

    public string Content { get; init; } = "{}";

    public string? EntityTag { get; init; }

    public TimeSpan? RetryAfter { get; init; }

    public long? DeclaredContentLength { get; init; }

    public bool ThrowTimeout { get; init; }

    public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.Requests.Add(CloneRequest(request));
        if (this.ThrowTimeout)
        {
            throw new TaskCanceledException("Simulated timeout.");
        }

        HttpResponseMessage response = new HttpResponseMessage(this.StatusCode)
        {
            Content = new StringContent(this.Content, Encoding.UTF8, "application/json"),
        };
        if (this.EntityTag is not null)
        {
            response.Headers.ETag = EntityTagHeaderValue.Parse(this.EntityTag);
        }

        if (this.RetryAfter.HasValue)
        {
            response.Headers.RetryAfter = new RetryConditionHeaderValue(this.RetryAfter.Value);
        }

        if (this.DeclaredContentLength.HasValue)
        {
            response.Content.Headers.ContentLength = this.DeclaredContentLength.Value;
        }

        return Task.FromResult(response);
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        HttpRequestMessage clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
