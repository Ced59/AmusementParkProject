using System.Net;
using System.Text.Json;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Infrastructure.Services.Seo;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Seo;

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly List<string> requestBodies;

    public RecordingHttpMessageHandler(List<string> requestBodies)
    {
        this.requestBodies = requestBodies;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        this.requestBodies.Add(requestBody);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
