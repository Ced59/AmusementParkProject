using System.Net;
using System.Text.Json;
using System.Text;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Services.Ssr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ssr;

internal sealed class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient httpClient;

    public TestHttpClientFactory(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        Assert.Equal(HttpTechnicalStatsProvider.HttpClientName, name);
        return this.httpClient;
    }
}
