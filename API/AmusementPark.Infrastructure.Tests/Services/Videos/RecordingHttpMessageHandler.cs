using System.Net;
using System.Text;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Configuration.Videos;
using AmusementPark.Infrastructure.Services.Videos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Videos;

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly string responseBody;

    public RecordingHttpMessageHandler(string responseBody)
    {
        this.responseBody = responseBody;
    }

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.LastRequestUri = request.RequestUri;
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(this.responseBody, Encoding.UTF8, "application/json"),
        };

        return Task.FromResult(response);
    }
}
