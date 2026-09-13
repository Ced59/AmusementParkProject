using System.Net;
using System.Text.Json;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Infrastructure.Services.Seo;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Seo;

internal sealed class RecordingHttpClientFactory : IHttpClientFactory
{
    public List<string> RequestBodies { get; } = new List<string>();

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new RecordingHttpMessageHandler(this.RequestBodies));
    }
}
