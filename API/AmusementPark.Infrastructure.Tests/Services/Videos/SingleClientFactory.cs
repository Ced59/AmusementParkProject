using System.Net;
using System.Text;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Configuration.Videos;
using AmusementPark.Infrastructure.Services.Videos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Videos;

internal sealed class SingleClientFactory : IHttpClientFactory
{
    private readonly HttpClient client;

    public SingleClientFactory(HttpClient client)
    {
        this.client = client;
    }

    public HttpClient CreateClient(string name)
    {
        return this.client;
    }
}
