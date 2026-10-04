using System.Net;
using System.Text.Json;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Infrastructure.Services.Seo;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Seo;

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly List<string> requestBodies;
    private readonly List<long?> requestContentLengths;
    private readonly List<string?> requestMediaTypes;

    public RecordingHttpMessageHandler(
        List<string> requestBodies,
        List<long?> requestContentLengths,
        List<string?> requestMediaTypes)
    {
        this.requestBodies = requestBodies;
        this.requestContentLengths = requestContentLengths;
        this.requestMediaTypes = requestMediaTypes;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        this.requestBodies.Add(requestBody);
        this.requestContentLengths.Add(request.Content?.Headers.ContentLength);
        this.requestMediaTypes.Add(request.Content?.Headers.ContentType?.MediaType);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
