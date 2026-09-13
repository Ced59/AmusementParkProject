using System.Net;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Services.Ssr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ssr;

internal sealed class StubHttpClientFactory : IHttpClientFactory
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
