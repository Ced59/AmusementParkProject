using System.Net;
using System.Text;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;
using AmusementPark.Infrastructure.Services.Weather;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Weather;

internal sealed class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient httpClient;

    public TestHttpClientFactory(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        return this.httpClient;
    }
}
